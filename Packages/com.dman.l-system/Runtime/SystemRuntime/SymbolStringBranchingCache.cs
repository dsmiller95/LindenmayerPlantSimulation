using Dman.LSystem.SystemCompiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime
{
    public class SymbolStringBranchingCache
    {
        public static int defaultBranchOpenSymbol = '[';
        public static int defaultBranchCloseSymbol = ']';

        public int branchOpenSymbol;
        public int branchCloseSymbol;

        private ISet<int> ignoreSymbols;
        /// <summary>
        /// Contains a caches set of indexes, mapping each branching symbol to its matching closing/opening symbol.
        /// </summary>
        private Dictionary<int, int> branchingJumpIndexes;

        public SymbolStringBranchingCache() : this(defaultBranchOpenSymbol, defaultBranchCloseSymbol, new HashSet<int>()) { }
        public SymbolStringBranchingCache(int open, int close, ISet<int> ignoreSymbols)
        {
            branchOpenSymbol = open;
            branchCloseSymbol = close;
            this.ignoreSymbols = ignoreSymbols;
        }

        public void BuildJumpIndexesFromSymbols(NativeArray<int> symbols)
        {
            branchingJumpIndexes = new Dictionary<int, int>();
            CacheAllBranchJumpIndexes(symbols);

        }

        public bool ValidForwardMatch(SymbolSeriesMatcher seriesMatch)
        {
            return true;
        }
        public bool ValidBackwardsMatch(SymbolSeriesMatcher seriesMatch)
        {
            return seriesMatch.targetSymbolSeries.All(x => x.targetSymbol != defaultBranchOpenSymbol && x.targetSymbol != defaultBranchCloseSymbol);
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
            SymbolSeriesMatcher seriesMatch,
            NativeArray<int> symbolHandle,
            NativeArray<SymbolString<float>.JaggedIndexing> parameterIndexingHandle)
        {
            if (seriesMatch.graphChildPointers == null)
            {
                // this should be done in the parsing/compiling phase. should only have to happen once for the whole system, per matching rule.
                throw new System.Exception("graph indexes should be precomputer");
                //seriesMatch.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);
            }

            // keep count of how many more matches are required at each level of the tree.
            //  starts out as a copy of the child count array. each leaf will be at 0, and will go negative when matched.
            //var remainingMatchesAtIndexes = seriesMatch.childrenCounts.Clone() as int[];
            var targetSymbolToMatchSymbol = MatchesAtIndexOrderingInvariant(
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
        /// <returns>A dictionary mapping indexes in the matcher to indexes in the symbol target, based on what symbols were matched</returns>
        public IDictionary<int, int> MatchesBackwards(
            int indexInSymbolTarget,
            SymbolSeriesMatcher seriesMatch,
            NativeArray<int> symbolHandle,
            NativeArray<SymbolString<float>.JaggedIndexing> parameterIndexingHandle)
        {
            indexInSymbolTarget--;
            int matchingIndex = seriesMatch.targetSymbolSeries.Length - 1;

            Dictionary<int, int>  matcherIndexToTargetIndex = null;

            for (; matchingIndex >= 0 && indexInSymbolTarget >= 0;)
            {
                var symbolToMatch = seriesMatch.targetSymbolSeries[matchingIndex];
                while (indexInSymbolTarget >= 0)
                {
                    var currentSymbol = symbolHandle[indexInSymbolTarget];
                    if (ignoreSymbols.Contains(currentSymbol) || currentSymbol == branchOpenSymbol)
                    {
                        indexInSymbolTarget--;
                    }else if (currentSymbol == branchCloseSymbol)
                    {
                        indexInSymbolTarget = FindOpeningBranchIndexReadonly(indexInSymbolTarget) - 1;
                    }
                    else if (
                        currentSymbol == symbolToMatch.targetSymbol &&
                        symbolToMatch.parameterLength == parameterIndexingHandle[indexInSymbolTarget].length)
                    {
                        if(matcherIndexToTargetIndex == null)
                        {
                            matcherIndexToTargetIndex = new Dictionary<int, int>();
                        }
                        matcherIndexToTargetIndex[matchingIndex] = indexInSymbolTarget;
                        indexInSymbolTarget--;
                        matchingIndex--;
                        break;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            if (matchingIndex == -1)
            {
                return matcherIndexToTargetIndex;
            }
            return null;
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
            }else
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
        public void CacheAllBranchJumpIndexes(NativeArray<int> symbols)
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
                    branchingJumpIndexes[indexInString] = correspondingOpenSymbolIndex;
                    branchingJumpIndexes[correspondingOpenSymbolIndex] = indexInString;
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
            public SymbolSeriesMatcher.DepthFirstSearchState matcherSymbolBranchingSearchState;
        }

        /// <summary>
        /// check for a match, enforcing the same ordering in the target match as defined in the matching pattern.
        /// </summary>
        /// <param name="originIndexInTarget"></param>
        /// <param name="seriesMatch"></param>
        /// <param name="consumedTargetIndexes"></param>
        /// <returns></returns>
        private ImmutableDictionary<int, int> MatchesAtIndexOrderingInvariant(
            int originIndexInTarget,
            SymbolSeriesMatcher seriesMatch,
            NativeArray<int> symbolHandle,
            NativeArray<SymbolString<float>.JaggedIndexing> parameterIndexingHandle)
        {
            var targetParentIndexStack = new Stack<BranchEventData>();
            int currentParentIndexInTarget = originIndexInTarget;
            var targetIndexesToMatchIndexes = ImmutableDictionary<int, int>.Empty;

            var indexInMatchDFSState = seriesMatch.GetImmutableDepthFirstIterationState();
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
            SymbolSeriesMatcher seriesMatch,
            ImmutableDictionary<int, int> targetIndexesToMatchIndexes,
            int currentParentIndexInTarget,
            int currentIndexInTarget,
            int currentIndexInMatch,
            NativeArray<int> symbolHandle,
            NativeArray<SymbolString<float>.JaggedIndexing> parameterIndexingHandle
            )
        {
            var symbolInMatch = seriesMatch.targetSymbolSeries[currentIndexInMatch];
            if (
                symbolInMatch.targetSymbol == symbolHandle[currentIndexInTarget] &&
                symbolInMatch.parameterLength == parameterIndexingHandle[currentIndexInTarget].length)
            {
                var parentIndexInMatch = seriesMatch.graphParentPointers[currentIndexInMatch];
                if(parentIndexInMatch == -1)
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
       
    }
}
