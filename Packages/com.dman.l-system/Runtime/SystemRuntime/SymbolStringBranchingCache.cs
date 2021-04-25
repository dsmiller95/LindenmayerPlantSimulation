using Dman.LSystem.SystemCompiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    // TODO: make struct, use native data structures
    public struct SymbolStringBranchingCache : System.IDisposable
    {
        public static int defaultBranchOpenSymbol = '[';
        public static int defaultBranchCloseSymbol = ']';

        public int branchOpenSymbol;
        public int branchCloseSymbol;

        [ReadOnly]
        private NativeHashSet<int> ignoreSymbols;
        /// <summary>
        /// Contains a caches set of indexes, mapping each branching symbol to its matching closing/opening symbol.
        /// </summary>
        [ReadOnly]
        private NativeHashMap<int, int> branchingJumpIndexes;
        private SystemLevelRuleNativeData nativeRuleData; // todo: will have to pass this in instead of attach, maybe

        public SymbolStringBranchingCache(SystemLevelRuleNativeData nativeRuleData)
            : this(defaultBranchOpenSymbol, defaultBranchCloseSymbol, new HashSet<int>(), nativeRuleData) { }
        public SymbolStringBranchingCache(
            int open, int close,
            ISet<int> ignoreSymbols,
            SystemLevelRuleNativeData nativeRuleData,
            Allocator allocator = Allocator.Persistent)
        {
            branchOpenSymbol = open;
            branchCloseSymbol = close;
            this.ignoreSymbols = new NativeHashSet<int>(ignoreSymbols.Count, allocator);
            foreach (var ignored in ignoreSymbols)
            {
                this.ignoreSymbols.Add(ignored);
            }
            this.nativeRuleData = nativeRuleData;

            branchingJumpIndexes = default;
        }

        public void BuildJumpIndexesFromSymbols(NativeArray<int> symbols)
        {
            var tmpBranchingJumpIndexes = new Dictionary<int, int>();
            CacheAllBranchJumpIndexes(symbols, tmpBranchingJumpIndexes);

            branchingJumpIndexes = new NativeHashMap<int, int>(tmpBranchingJumpIndexes.Count, Allocator.Persistent);
            foreach (var kvp in tmpBranchingJumpIndexes)
            {
                branchingJumpIndexes[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// partial, semi-ordered tree matching algorithm.
        /// 
        /// iterate over target string, no backtracking
        /// maintain data about matches in series match:
        ///     indexes which are valid branched matches for the current index in target
        ///     record of which leaves in the matcher have been matched
        /// </summary>
        /// <param name="indexInSymbolTarget"></param>
        /// <param name="seriesMatch"></param>
        /// <returns>a mapping from all symbols in seriesMatch back into the target string</returns>
        public IDictionary<int, int> MatchesForward(
            int indexInSymbolTarget,
            SymbolSeriesSuffixMatcher seriesMatch,
            NativeArray<int> symbolHandle,
            NativeArray<JaggedIndexing> parameterIndexingHandle)
        {
            if (!seriesMatch.HasGraphIndexes)
            {
                // this should be done in the parsing/compiling phase. should only have to happen once for the whole system, per matching rule.
                throw new System.Exception("graph indexes should be precomputed");
                //seriesMatch.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);
            }

            // keep count of how many more matches are required at each level of the tree.
            //  starts out as a copy of the child count array. each leaf will be at 0, and will go negative when matched.
            //var remainingMatchesAtIndexes = seriesMatch.childrenCounts.Clone() as int[];
            var targetSymbolToMatchSymbol = MatchesForwardsAtIndexOrderingInvariant(
                indexInSymbolTarget,
                seriesMatch,
                symbolHandle,
                parameterIndexingHandle);
            // order by target symbol, then aggregate to dictionary. there will be duplicates of match indexes,
            //  ordering this way guarantees that the only last duplicate in the target sting will be preserved.
            //  Since the method is order invariant, it is guaranteed that the match with the highest index in the target
            //  string is the correct match.
            if (targetSymbolToMatchSymbol == null)
            {
                return null;
            }
            var resultBuilder = ImmutableDictionary<int, int>.Empty.ToBuilder();
            foreach (var kvp in targetSymbolToMatchSymbol.OrderBy(x => x.Key))
            {
                resultBuilder[kvp.Value] = kvp.Key;
            }
            return resultBuilder.ToImmutable();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexInSymbolTarget"></param>
        /// <param name="seriesMatch"></param>
        /// <returns>whether the match succeeded or not</returns>
        public bool MatchesBackwards(
            int indexInSymbolTarget,
            SymbolSeriesPrefixMatcher seriesMatch,
            SymbolString<float> symbolString,
            int firstParameterCopyIndex,
            NativeArray<float> parameterCopyMemory,
            out byte paramsCopiedToMem)
        {
            indexInSymbolTarget--;
            int matchingIndex = seriesMatch.graphNodeMemSpace.length - 1;

            paramsCopiedToMem = 0;

            for (; matchingIndex >= 0 && indexInSymbolTarget >= 0;)
            {
                var symbolToMatch = nativeRuleData.prefixMatcherSymbols[matchingIndex + seriesMatch.graphNodeMemSpace.index];
                while (indexInSymbolTarget >= 0)
                {
                    var currentSymbol = symbolString.symbols[indexInSymbolTarget];
                    if (ignoreSymbols.Contains(currentSymbol) || currentSymbol == branchOpenSymbol)
                    {
                        indexInSymbolTarget--;
                    }
                    else if (currentSymbol == branchCloseSymbol)
                    {
                        indexInSymbolTarget = FindOpeningBranchIndexReadonly(indexInSymbolTarget) - 1;
                    }
                    else if (currentSymbol == symbolToMatch.targetSymbol &&
                        symbolToMatch.parameterLength == symbolString.newParameters[indexInSymbolTarget].length)
                    {
                        // copy the parameters in reverse order, so they can be reversed in-place at end
                        //  on success
                        var paramIndexing = symbolString.newParameters[indexInSymbolTarget];
                        for (int i = paramIndexing.length - 1; i >= 0; i--)
                        {
                            parameterCopyMemory[paramsCopiedToMem + firstParameterCopyIndex] = symbolString.newParameters[paramIndexing, i];
                            paramsCopiedToMem++;
                        }

                        indexInSymbolTarget--;
                        matchingIndex--;
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (matchingIndex == -1)
            {
                ReverseRange(parameterCopyMemory, firstParameterCopyIndex, paramsCopiedToMem);
                return true;
            }
            return false;
        }

        public static void ReverseRange<T>(NativeArray<T> data, int firstIndex, int length) where T: unmanaged
        {
            for (int i = 0; i < length/2; i++)
            {
                var swap0 = i + firstIndex;
                var swap1 = length - 1 - i + firstIndex;
                var swap = data[swap0];
                data[swap0] = data[swap1];
                data[swap1] = swap;
            }
        }


        /// <summary>
        /// Assumes that <paramref name="openingBranchIndex"/> is already an index of an open branch symbol, without checking
        ///     will return info without modifying the state of this object
        /// </summary>
        /// <param name="openingBranchIndex"></param>
        /// <returns></returns>
        public int FindClosingBranchIndexReadonly(int openingBranchIndex)
        {
            if (branchingJumpIndexes.TryGetValue(openingBranchIndex, out var closingBranch))
            {
                return closingBranch;
            }
            else
            {
                throw new System.Exception("branch jump index not cached!! should preload.");
            }
        }

        /// <summary>
        /// Assumes that <paramref name="closingBranchIndex"/> is already an index of a closing branch symbol, without checking
        ///     will return info without modifying the state of this object
        /// </summary>
        /// <param name="closingBranchIndex"></param>
        /// <returns></returns>
        public int FindOpeningBranchIndexReadonly(int closingBranchIndex)
        {
            if (branchingJumpIndexes.TryGetValue(closingBranchIndex, out var openingBranch))
            {
                return openingBranch;
            }
            else
            {
                throw new System.Exception("branch jump index not cached!! should preload.");
            }
        }

        /// <summary>
        /// Read through the entire current symbol string, and cache the jump indexes for all branching symbols
        ///     assumes no branches are cached already
        /// </summary>
        /// <returns></returns>
        public void CacheAllBranchJumpIndexes(
            NativeArray<int> symbols,
            Dictionary<int, int> jumpIndexes)
        {
            var openingIndexes = new Stack<int>();

            for (int indexInString = 0; indexInString < symbols.Length; indexInString++)
            {
                var symbol = symbols[indexInString];
                if (symbol == branchOpenSymbol)
                {
                    openingIndexes.Push(indexInString);
                }
                else if (symbol == branchCloseSymbol)
                {
                    if (openingIndexes.Count <= 0)
                    {
                        throw new SyntaxException("Too many closing branch symbols. malformed symbol string.");
                    }
                    var correspondingOpenSymbolIndex = openingIndexes.Pop();
                    jumpIndexes[indexInString] = correspondingOpenSymbolIndex;
                    jumpIndexes[correspondingOpenSymbolIndex] = indexInString;
                }
            }
            if (openingIndexes.Count != 0)
            {
                throw new SyntaxException("Too many opening branch symbols. malformed symbol string.");
            }
        }

        private struct BranchEventData
        {
            public int currentParentIndex;
            public int openBranchSymbolIndex;
            public SymbolSeriesSuffixMatcher.DepthFirstSearchState matcherSymbolBranchingSearchState;
        }

        /// <summary>
        /// check for a match, enforcing the same ordering in the target match as defined in the matching pattern.
        /// </summary>
        /// <param name="originIndexInTarget"></param>
        /// <param name="seriesMatch"></param>
        /// <param name="consumedTargetIndexes"></param>
        /// <returns></returns>
        private ImmutableDictionary<int, int> MatchesForwardsAtIndexOrderingInvariant(
            int originIndexInTarget,
            SymbolSeriesSuffixMatcher seriesMatch,
            NativeArray<int> symbolHandle,
            NativeArray<JaggedIndexing> parameterIndexingHandle)
        {
            var targetParentIndexStack = new Stack<BranchEventData>();
            int currentParentIndexInTarget = originIndexInTarget;
            var targetIndexesToMatchIndexes = ImmutableDictionary<int, int>.Empty;

            var indexInMatchDFSState = seriesMatch.GetImmutableDepthFirstIterationState(nativeRuleData);
            targetIndexesToMatchIndexes = targetIndexesToMatchIndexes.Add(originIndexInTarget, indexInMatchDFSState.currentIndex);
            if (!indexInMatchDFSState.Next(out indexInMatchDFSState))
            {
                // if the match is empty, automatically matches.
                return targetIndexesToMatchIndexes;
            }



            for (int indexInTarget = originIndexInTarget + 1; indexInTarget < symbolHandle.Length; indexInTarget++)
            {
                var targetSymbol = symbolHandle[indexInTarget];

                if (ignoreSymbols.Contains(targetSymbol))
                {
                    continue;
                }
                if (targetSymbol == branchOpenSymbol)
                {
                    targetParentIndexStack.Push(new BranchEventData
                    {
                        currentParentIndex = currentParentIndexInTarget,
                        openBranchSymbolIndex = indexInTarget,
                        matcherSymbolBranchingSearchState = indexInMatchDFSState
                    });
                }
                else if (targetSymbol == branchCloseSymbol)
                {
                    // will encounter a close symbol in one of two cases:
                    //  1. the branch in target has exactly matched the branch in the matcher, and we should just step down
                    //  2. the branch in target has terminated early, meaning we must step down the branch chain and also
                    //      reverse the matcher DFS back to a common ancenstor
                    if (targetParentIndexStack.Count <= 0)
                    {
                        // if we encounter the end of the branch which contains the origin index before full match, fail.
                        return null;
                    }
                    var lastBranch = targetParentIndexStack.Pop();
                    currentParentIndexInTarget = lastBranch.currentParentIndex;
                }
                else
                {
                    // reverse the DFS in matcher, back to the last point which shares a parent with the current parent
                    // this acts to ensure the entry to the match has a shared parent, if at all possible.
                    //   the reversal is necessary when a branching structure failed to match in the last step
                    var parentInMatch = targetIndexesToMatchIndexes[currentParentIndexInTarget];
                    if (indexInMatchDFSState.FindPreviousWithParent(out var reversedMatchIndex, parentInMatch))
                    {
                        indexInMatchDFSState = reversedMatchIndex;
                    }

                    var indexInMatch = indexInMatchDFSState.currentIndex;
                    var currentTargetMatchesMatcher = TargetSymbolMatchesAndParentMatches(
                        seriesMatch,
                        targetIndexesToMatchIndexes,
                        currentParentIndexInTarget,
                        indexInTarget,
                        indexInMatch,
                        symbolHandle,
                        parameterIndexingHandle);
                    if (currentTargetMatchesMatcher)
                    {
                        targetIndexesToMatchIndexes = targetIndexesToMatchIndexes.Add(indexInTarget, indexInMatch);
                        currentParentIndexInTarget = indexInTarget; // series continuation includes implicit parenting
                        if (!indexInMatchDFSState.Next(out indexInMatchDFSState))
                        {
                            return targetIndexesToMatchIndexes;
                        }
                    }
                    else
                    {
                        // symbol in target isn't a valid match, so no further symbols in the current target branching structure can match.
                        // rewind back to the previous branching symbol, and skip this whole structure.
                        // Or if we're not in a nested structure, fail.
                        if (targetParentIndexStack.Count <= 0)
                        {
                            return null;
                        }
                        var lastBranch = targetParentIndexStack.Pop();
                        currentParentIndexInTarget = lastBranch.currentParentIndex;
                        indexInTarget = FindClosingBranchIndexReadonly(lastBranch.openBranchSymbolIndex);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Checks to see if <paramref name="currentSymbolInTarget"/> matches the symbol at <paramref name="currentIndexInMatch"/> in the <paramref name="seriesMatch"/> string.
        ///     And also if the parent at <paramref name="currentParentIndexInTarget"/> maps to the parent of <paramref name="currentIndexInMatch"/>
        /// </summary>
        /// <param name="seriesMatch"></param>
        /// <param name="targetIndexesToMatchIndexes"></param>
        /// <param name="currentParentIndexInTarget"></param>
        /// <param name="currentSymbolInTarget"></param>
        /// <param name="currentIndexInMatch"></param>
        /// <returns></returns>
        private bool TargetSymbolMatchesAndParentMatches(
            SymbolSeriesSuffixMatcher seriesMatch,
            ImmutableDictionary<int, int> targetIndexesToMatchIndexes,
            int currentParentIndexInTarget,
            int currentIndexInTarget,
            int currentIndexInMatch,
            NativeArray<int> symbolHandle,
            NativeArray<JaggedIndexing> parameterIndexingHandle
            )
        {
            var symbolInMatch = nativeRuleData.suffixMatcherGraphNodeData[currentIndexInMatch + seriesMatch.graphNodeMemSpace.index].mySymbol;
            if (
                symbolInMatch.targetSymbol == symbolHandle[currentIndexInTarget] &&
                symbolInMatch.parameterLength == parameterIndexingHandle[currentIndexInTarget].length)
            {
                var parentIndexInMatch = nativeRuleData.suffixMatcherGraphNodeData[currentIndexInMatch + seriesMatch.graphNodeMemSpace.index].parentIndex;
                if (parentIndexInMatch == -1)
                {
                    // if the parent is the origin, always match.
                    return true;
                }
                var parentIndexInTarget = currentParentIndexInTarget;
                var indexInMatchOfTargetParentMapped = targetIndexesToMatchIndexes[parentIndexInTarget];
                // check to ensure the parent of this node in the target graph
                //  is already mapped to the parent of the node in the 
                if (parentIndexInMatch == indexInMatchOfTargetParentMapped)
                {
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            branchingJumpIndexes.Dispose();
            ignoreSymbols.Dispose();
        }
    }
}
