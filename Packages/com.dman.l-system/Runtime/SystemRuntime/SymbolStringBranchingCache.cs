using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{
    internal class SymbolStringBranchingCache
    {
        public static int defaultBranchOpenSymbol = '[';
        public static int defaultBranchCloseSymbol = ']';

        public int branchOpenSymbol;
        public int branchCloseSymbol;

        private ISymbolString symbolStringTarget;
        /// <summary>
        /// Contains a caches set of indexes, mapping each branching symbol to its matching closing/opening symbol.
        /// </summary>
        private Dictionary<int, int> branchingJumpIndexes;

        public SymbolStringBranchingCache() : this(defaultBranchOpenSymbol, defaultBranchCloseSymbol) { }
        public SymbolStringBranchingCache(int open, int close)
        {
            branchOpenSymbol = open;
            branchCloseSymbol = close;
        }

        public void SetTargetSymbolString(ISymbolString symbols)
        {
            symbolStringTarget = symbols;
            branchingJumpIndexes = new Dictionary<int, int>();
        }

        public bool ValidForwardMatch(SymbolSeriesMatcher seriesMatch)
        {
            return true;
        }
        public bool ValidBackwardsMatch(SymbolSeriesMatcher seriesMatch)
        {
            return seriesMatch.targetSymbolSeries.All(x => x.targetSymbol != defaultBranchOpenSymbol && x.targetSymbol != defaultBranchCloseSymbol);
        }

        struct MatchCheckPoint
        {
            public int nextIndexInTargetToCheck;
            public int nextIndexInMatchToCheck;
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
        public IDictionary<int, int> MatchesForward(int indexInSymbolTarget, SymbolSeriesMatcher seriesMatch, bool orderingAgnostict)
        {
            if (seriesMatch.graphChildPointers == null)
            {
                // TODO: consider doing this in the parsing/compiling phase. should only have to happen once for the whole system.
                seriesMatch.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);
            }

            // keep count of how many more matches are required at each level of the tree.
            //  starts out as a copy of the child count array. each leaf will be at 0, and will go negative when matched.
            //var remainingMatchesAtIndexes = seriesMatch.childrenCounts.Clone() as int[];

            if (orderingAgnostict)
            {
                var remappedSymbols = ImmutableDictionary<int, int>.Empty;
                var matchedSet = MatchesAtIndexOrderAgnostic(new MatchCheckPoint
                {
                    nextIndexInMatchToCheck = -1,
                    nextIndexInTargetToCheck = indexInSymbolTarget
                },
                seriesMatch,
                remappedSymbols);

                // invert the dictionary here, switching from mapping from target string into matcher string
                // to mapping from the matcher string into the target string
                return matchedSet?.ToDictionary(x => x.Value, x => x.Key);
            } else
            {
                var targetSymbolToMatchSymbol = MatchesAtIndexOrderingInvariant(indexInSymbolTarget, seriesMatch);
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
                    //resultBuilder.Add(kvp.Value, kvp.Key);
                }
                return resultBuilder.ToImmutable();
            }

            //return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="nextCheck"></param>
        /// <param name="seriesMatch"></param>
        /// <param name="consumedTargetIndexes">
        ///     indexes of symbols in the target string which have been matched against. These indexes will never be used as
        ///     part of any match
        /// </param>
        /// <returns>
        ///     a dictionary of the indexes in the target which have been consumed as a result of the matches, mapped to the index of the symbol in the matching pattern.
        ///     null if no match.
        /// </returns>
        private ImmutableDictionary<int, int> MatchesAtIndexOrderAgnostic(
            MatchCheckPoint nextCheck,
            SymbolSeriesMatcher seriesMatch,
            ImmutableDictionary<int, int> consumedTargetIndexes)
        {
            if (nextCheck.nextIndexInTargetToCheck >= symbolStringTarget.Length)
            {
                return null;
            }
            if (consumedTargetIndexes.ContainsKey(nextCheck.nextIndexInTargetToCheck))
            {
                return null;
            }
            int nextTargetSymbol = symbolStringTarget[nextCheck.nextIndexInTargetToCheck];
            if (nextTargetSymbol == branchOpenSymbol)
            {
                // branch open. don't compare against matcher, just advance.
                //   only forward through matcher indexes
                // for every branching structure in target,
                //  check if any matches starting at the matcher index passed in
                foreach (var childIndexInTarget in GetIndexesInTargetOfAllChildren(nextCheck.nextIndexInTargetToCheck))
                {
                    var matchesAtChild = MatchesAtIndexOrderAgnostic(
                        new MatchCheckPoint
                        {
                            nextIndexInTargetToCheck = childIndexInTarget,
                            nextIndexInMatchToCheck = nextCheck.nextIndexInMatchToCheck
                        },
                        seriesMatch,
                        consumedTargetIndexes);
                    if (matchesAtChild != null)
                    {
                        return consumedTargetIndexes.AddRange(matchesAtChild);
                    }
                }
                return null;
            }
            else
            {
                if (nextCheck.nextIndexInMatchToCheck != -1)
                {
                    // if its the Origin symbol, don't even check if match. assume it does, proceed to children checking.
                    var matchingSymbol = seriesMatch.targetSymbolSeries[nextCheck.nextIndexInMatchToCheck].targetSymbol;
                    if (matchingSymbol != nextTargetSymbol)
                    {
                        return null;
                    }
                }
                var childrenBranchesToMatch = seriesMatch.graphChildPointers[nextCheck.nextIndexInMatchToCheck + 1].Select(x => x - 1).ToArray();
                bool[] matchedChildren = new bool[childrenBranchesToMatch.Length];

                foreach (var childIndexInTarget in GetIndexesInTargetOfAllChildren(nextCheck.nextIndexInTargetToCheck))
                {
                    // TODO: early exit if all branches matched
                    for (int branchMatchIndex = 0; branchMatchIndex < childrenBranchesToMatch.Length; branchMatchIndex++)
                    {
                        if (matchedChildren[branchMatchIndex])
                            continue; // if branch matched already, skip.
                        var consumedMatchesHere = MatchesAtIndexOrderAgnostic(
                            new MatchCheckPoint
                            {
                                nextIndexInMatchToCheck = childrenBranchesToMatch[branchMatchIndex],
                                nextIndexInTargetToCheck = childIndexInTarget
                            },
                            seriesMatch,
                            consumedTargetIndexes);
                        if (consumedMatchesHere != null)
                        {
                            matchedChildren[branchMatchIndex] = true;
                            // tracking consumed targets here is necessary because each branch is capable of matching multiple matcher patterns,
                            //  by containing internal nesting.for example a target string of A[[B]B][B] will match a matching pattern of A[B][B][B].
                            //  consumed symbols is used to ensure all 3 of the unique branches in the pattern do not match on the same first occurence of B
                            consumedTargetIndexes = consumedTargetIndexes.AddRange(consumedMatchesHere);
                        }
                    }
                }

                if (matchedChildren.All(x => x))
                {
                    // all branches matched. success!
                    return consumedTargetIndexes.Add(nextCheck.nextIndexInTargetToCheck, nextCheck.nextIndexInMatchToCheck);
                }
                return null;
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
            SymbolSeriesMatcher seriesMatch)
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



            for (int indexInTarget = originIndexInTarget + 1; indexInTarget < symbolStringTarget.Length; indexInTarget++)
            {
                var targetSymbol = symbolStringTarget[indexInTarget];
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
                    // cache the open/close braces, may as well while we're here.
                    branchingJumpIndexes[lastBranch.openBranchSymbolIndex] = indexInTarget;
                    branchingJumpIndexes[indexInTarget] = lastBranch.openBranchSymbolIndex;
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
                        targetSymbol,
                        indexInMatch);
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
                        indexInTarget = FindClosingBranchIndex(lastBranch.openBranchSymbolIndex);
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
            int currentSymbolInTarget,
            int currentIndexInMatch
            )
        {
            var symbolInMatch = seriesMatch.targetSymbolSeries[currentIndexInMatch].targetSymbol;
            if (symbolInMatch == currentSymbolInTarget)
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

        /// <summary>
        /// returns indexes of all structures which can be children of the symbol at <paramref name="originIndex"/>
        /// will return 1, 4 when run starting at 0 for string: "A[B]CD"
        /// </summary>
        /// <param name="originIndex"></param>
        /// <returns></returns>
        private IEnumerable<int> GetIndexesInTargetOfAllChildren(int originIndex)
        {
            int childStructureIndexInTarget = originIndex + 1;
            // for every branching structure in target,
            //  check if any matches starting at the matcher index passed in
            while (childStructureIndexInTarget < symbolStringTarget.Length
                && symbolStringTarget[childStructureIndexInTarget] == branchOpenSymbol)
            {
                yield return childStructureIndexInTarget;
                childStructureIndexInTarget = FindClosingBranchIndex(childStructureIndexInTarget) + 1;
            }
            if (childStructureIndexInTarget < symbolStringTarget.Length)
            {
                yield return childStructureIndexInTarget;
            }
        }
        public bool MatchesBackwards(int indexInSymbolTarget, SymbolSeriesMatcher seriesMatch)
        {
            indexInSymbolTarget--;
            int matchingIndex = seriesMatch.targetSymbolSeries.Length - 1;

            for (; matchingIndex >= 0 && indexInSymbolTarget >= 0; matchingIndex--)
            {
                var symbolToMatch = seriesMatch.targetSymbolSeries[matchingIndex].targetSymbol;
                while (indexInSymbolTarget >= 0)
                {
                    var currentSymbol = symbolStringTarget[indexInSymbolTarget];
                    if (currentSymbol == branchCloseSymbol)
                    {
                        indexInSymbolTarget = FindOpeningBranchIndex(indexInSymbolTarget) - 1;
                    }
                    else if (currentSymbol == branchOpenSymbol)
                    {
                        indexInSymbolTarget--;
                    }
                    else if (currentSymbol == symbolToMatch)
                    {
                        indexInSymbolTarget--;
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return matchingIndex == -1;
        }

        /// <summary>
        /// Assumes that <paramref name="openingBranchIndex"/> is already an index of an open branch symbol, without checking
        /// </summary>
        /// <param name="openingBranchIndex"></param>
        /// <returns></returns>
        public int FindClosingBranchIndex(int openingBranchIndex)
        {
            if (branchingJumpIndexes.TryGetValue(openingBranchIndex, out var closingBranch))
            {
                return closingBranch;
            }
            var openingIndexes = new Stack<int>();
            openingIndexes.Push(openingBranchIndex);

            for (int indexInString = openingBranchIndex + 1; indexInString < symbolStringTarget.Length; indexInString++)
            {
                var symbol = symbolStringTarget[indexInString];
                if (symbol == branchOpenSymbol)
                {
                    openingIndexes.Push(symbol);
                }
                else if (symbol == branchCloseSymbol)
                {
                    var correspondingOpenSymbolIndex = openingIndexes.Pop();
                    branchingJumpIndexes[indexInString] = correspondingOpenSymbolIndex;
                    branchingJumpIndexes[correspondingOpenSymbolIndex] = indexInString;
                    if (openingIndexes.Count == 0)
                    {
                        return indexInString;
                    }
                }
            }
            throw new System.Exception("No matching closing branch found. malformed symbol string.");
        }

        /// <summary>
        /// Assumes that <paramref name="closingBranchIndex"/> is already an index of a closing branch symbol, without checking
        /// </summary>
        /// <param name="closingBranchIndex"></param>
        /// <returns></returns>
        public int FindOpeningBranchIndex(int closingBranchIndex)
        {
            if (branchingJumpIndexes.TryGetValue(closingBranchIndex, out var openingBranch))
            {
                return openingBranch;
            }
            var closingIndexes = new Stack<int>();
            closingIndexes.Push(closingBranchIndex);

            for (int indexInString = closingBranchIndex - 1; indexInString >= 0; indexInString--)
            {
                var symbol = symbolStringTarget[indexInString];
                if (symbol == branchCloseSymbol)
                {
                    closingIndexes.Push(symbol);
                }
                else if (symbol == branchOpenSymbol)
                {
                    var correspondingCloseSymbolIndex = closingIndexes.Pop();
                    branchingJumpIndexes[indexInString] = correspondingCloseSymbolIndex;
                    branchingJumpIndexes[correspondingCloseSymbolIndex] = indexInString;
                    if (closingIndexes.Count == 0)
                    {
                        return indexInString;
                    }
                }
            }
            throw new System.Exception("No matching opening branch found. malformed symbol string.");
        }
    }
}
