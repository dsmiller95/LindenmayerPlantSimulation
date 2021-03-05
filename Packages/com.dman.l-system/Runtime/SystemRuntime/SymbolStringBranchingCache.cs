using System.Collections.Generic;
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

        public bool MatchesForward(int indexInSymbolTarget, SymbolSeriesMatcher seriesMatch)
        {
            indexInSymbolTarget++;
            int matchingIndex = 0;

            for (; matchingIndex < seriesMatch.targetSymbolSeries.Length && indexInSymbolTarget < symbolStringTarget.Length; matchingIndex++)
            {
                var symbolToMatch = seriesMatch.targetSymbolSeries[matchingIndex].targetSymbol;
                while (indexInSymbolTarget < symbolStringTarget.Length)
                {
                    var currentSymbol = symbolStringTarget[indexInSymbolTarget];
                    if (currentSymbol == branchCloseSymbol)
                    {
                        return false;
                    }
                    else if (currentSymbol == branchOpenSymbol)
                    {
                        indexInSymbolTarget = FindClosingBranchIndex(indexInSymbolTarget) + 1;
                    }
                    else if (currentSymbol == symbolToMatch)
                    {
                        indexInSymbolTarget++;
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return matchingIndex == seriesMatch.targetSymbolSeries.Length;
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
                    }else if (currentSymbol == branchOpenSymbol)
                    {
                        indexInSymbolTarget--;
                    }else if (currentSymbol == symbolToMatch)
                    {
                        indexInSymbolTarget--;
                        break;
                    }else
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
