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
            public int lastMatchedIndexInTarget;
            public int lastMatchedIndexInMatcher;
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
        /// <returns></returns>
        public bool MatchesForward(int indexInSymbolTarget, SymbolSeriesMatcher seriesMatch)
        {
            if (seriesMatch.graphChildPointers == null)
            {
                // TODO: consider doing this in the parsing/compiling phase. should only have to happen once for the whole system.
                seriesMatch.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);
            }

            // keep count of how many more matches are required at each level of the tree.
            //  starts out as a copy of the child count array. each leaf will be at 0, and will go negative when matched.
            //var remainingMatchesAtIndexes = seriesMatch.childrenCounts.Clone() as int[];

            var consumedTargets = ImmutableHashSet<int>.Empty;
            var matchedSet = MatchesAtIndex(new MatchCheckPoint
            {
                lastMatchedIndexInMatcher = -1,
                lastMatchedIndexInTarget = indexInSymbolTarget
            },
            seriesMatch,
            consumedTargets);
            return matchedSet != null;

            //return false;
        }

        /// <summary>
        /// Entry conditions:
        ///     the symbols indexed by <paramref name="nextCheck"/> have already been compared, and do match.
        ///     The symbols indexed by <paramref name="nextCheck"/> are not present in <paramref name="consumedTargetIndexes"/>
        /// </summary>
        /// <param name="nextCheck"></param>
        /// <param name="seriesMatch"></param>
        /// <param name="consumedTargetIndexes">
        ///     indexes of symbols in the target string which have been matched against. These indexes will never be used as
        ///     part of any match
        /// </param>
        /// <returns>a set of the indexes in the target which have been consumed as a result of the matches. null if no match.</returns>
        private ImmutableHashSet<int> MatchesAtIndex(MatchCheckPoint nextCheck, SymbolSeriesMatcher seriesMatch, ImmutableHashSet<int> consumedTargetIndexes)
        {
            var children = seriesMatch.graphChildPointers[nextCheck.lastMatchedIndexInMatcher + 1];
            if (children.Length == 0)
            {
                // wat do when terminated?
                // probably just return success, since subset matching. Termination of matcher tree
                //  without mismatch means success
                return consumedTargetIndexes;
            }

            var nextTargetIndex = nextCheck.lastMatchedIndexInTarget + 1;
            if(nextTargetIndex >= symbolStringTarget.Length)
            {
                return null;
            }

            int nextTargetSymbol = symbolStringTarget[nextTargetIndex];

            // skip over all branching structures marked as consumed
            while (nextTargetIndex < symbolStringTarget.Length
                && consumedTargetIndexes.Contains(nextTargetIndex)
                && (nextTargetSymbol = symbolStringTarget[nextTargetIndex]) == branchOpenSymbol)
            {
                nextTargetIndex = FindClosingBranchIndex(nextTargetIndex) + 1;
            }
            if (consumedTargetIndexes.Contains(nextTargetIndex))
            {
                return null;
            }


            if (nextTargetSymbol == branchOpenSymbol)
            {
                var matchesInsideDirectNested = MatchesAtIndex(
                    new MatchCheckPoint
                    {
                        lastMatchedIndexInMatcher = nextCheck.lastMatchedIndexInMatcher,
                        lastMatchedIndexInTarget = nextTargetIndex
                    }, seriesMatch, consumedTargetIndexes);
                if (matchesInsideDirectNested != null)
                {
                    return consumedTargetIndexes.Union(matchesInsideDirectNested).Add(nextTargetIndex);
                }
            }

            int[] matcherBranches = children
                .Select(x => x - 1)
                .Where(x => seriesMatch.targetSymbolSeries[x].targetSymbol == branchOpenSymbol)
                .ToArray();
            // tracks the child branches in the matcher that have been matched against
            bool[] matchedBranches = new bool[matcherBranches.Length];
            // TODO: sort the branches by a complexity constant, matching most complex branches first.
            //   can be analized as part of the parent/child branching parser.

            bool hasSeriesContinuation = matcherBranches.Length < children.Length;
            int seriesContinuationMatcherIndex = -2;
            if (hasSeriesContinuation)
            {
                seriesContinuationMatcherIndex = children[children.Length - 1] - 1;
            }


            if (matcherBranches.Length > 0)
            {
                if (nextTargetSymbol != branchOpenSymbol)
                {
                    return null;
                }
                //var consumedIndexesBuilder = consumedTargetSymbols.ToBuilder();

                // for every branching structure in target,
                //  check all of the matcher's child branches against each
                while (nextTargetIndex < symbolStringTarget.Length
                    && (nextTargetSymbol = symbolStringTarget[nextTargetIndex]) == branchOpenSymbol)
                {
                    // TODO: early exit if all branches matched
                    for (int branchMatchIndex = 0; branchMatchIndex < matcherBranches.Length; branchMatchIndex++)
                    {
                        if (matchedBranches[branchMatchIndex])
                            continue; // if branch matched already, skip.
                        var consumedMatchesHere = MatchesAtIndex(
                            new MatchCheckPoint
                            {
                                lastMatchedIndexInMatcher = matcherBranches[branchMatchIndex],
                                lastMatchedIndexInTarget = nextTargetIndex
                            },
                            seriesMatch,
                            consumedTargetIndexes);
                        if(consumedMatchesHere != null)
                        {
                            matchedBranches[branchMatchIndex] = true;
                            // tracking consumed targets here is necessary because each branch is capable of matching multiple matcher patterns,
                            //  by containing internal nesting.for example a target string of A[[B]B][B] will match a matching pattern of A[B][B][B].
                            //  consumed symbols is used to ensure all 3 of the unique branches in the pattern do not match on the same first occurence of B
                            consumedTargetIndexes = consumedTargetIndexes.Union(consumedMatchesHere);
                        }
                    }
                    consumedTargetIndexes = consumedTargetIndexes.Add(nextTargetIndex);

                    nextTargetIndex = FindClosingBranchIndex(nextTargetIndex) + 1;
                }

                if (!matchedBranches.All(x => x))
                {
                    // not all branches matched. fail.
                    return null;
                }
            }
            else
            {
                // skip over all branching structures in target
                while (nextTargetIndex < symbolStringTarget.Length
                    && (nextTargetSymbol = symbolStringTarget[nextTargetIndex]) == branchOpenSymbol)
                {
                    nextTargetIndex = FindClosingBranchIndex(nextTargetIndex) + 1;
                }
            }
            if (!hasSeriesContinuation || seriesContinuationMatcherIndex < 0)
            {
                // no series continuation. we're done
                return consumedTargetIndexes;
            }
            if (nextTargetIndex >= symbolStringTarget.Length)
            {
                // matcher series continues, but target string has terminated.
                return null;
            }

            var nextMatcherSymbol = seriesMatch.targetSymbolSeries[seriesContinuationMatcherIndex].targetSymbol;


            if (nextMatcherSymbol == nextTargetSymbol)
            {
                var continuedMatch = MatchesAtIndex(new MatchCheckPoint
                {
                    lastMatchedIndexInMatcher = seriesContinuationMatcherIndex,
                    lastMatchedIndexInTarget = nextTargetIndex
                }, seriesMatch, consumedTargetIndexes);
                if (continuedMatch != null)
                {
                    //TODO: how handle deep recursion, which requires these changes to be undone?
                    return continuedMatch.Add(nextTargetIndex);
                }else
                {
                    return null;
                }
            }
            else
            {
                return null;
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
