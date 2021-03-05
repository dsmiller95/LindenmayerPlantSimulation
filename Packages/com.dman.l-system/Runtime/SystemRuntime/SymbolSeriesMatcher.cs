using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{

    internal struct GraphVisitIndex
    {
        public int grandParentIndex;
        public int parentIndex;
        public int parentSymbol;
        public int visitIndex;
    }

    internal class SymbolSeriesMatcher
    {
        public InputSymbol[] targetSymbolSeries;


        /// <summary>
        /// Jagged array used to represent the branching structure of <see cref="targetSymbolSeries"/>. 
        /// First element is entry point into the symbols, and does not represent any symbol in itself.
        ///     This array is shifted behind targetSymbolSeries by one. meaning that <see cref="graphPointers"/>[1]
        ///     refers to symbol <see cref="targetSymbolSeries"/>[0]
        /// </summary>
        public int[][] graphPointers;
        public void ComputeGraphIndexes(int branchOpen, int branchClose)
        {
            // indexes of parents. -1 is valid, indicating the parent is the entry point to the graph.
            // -2 is invalid, and indicates a node with no parent worth noting.
            var parentIndexes = new int[targetSymbolSeries.Length];
            var childrenCounts = new int[targetSymbolSeries.Length];
            var parentIndexStack = new Stack<int>();
            parentIndexStack.Push(-1);
            for (int indexInSymbols = 0; indexInSymbols < targetSymbolSeries.Length; indexInSymbols++)
            {
                var targetSymbol = targetSymbolSeries[indexInSymbols].targetSymbol;
                if(targetSymbol == branchOpen)
                {
                    var parentIndex = parentIndexStack.Peek();
                    parentIndexes[indexInSymbols] = parentIndex;
                    if(parentIndex >= 0)
                    {
                        childrenCounts[parentIndex]++;
                    }
                    parentIndexStack.Push(indexInSymbols);
                }else if (targetSymbol == branchClose)
                {
                    parentIndexes[indexInSymbols] = -2;
                    parentIndexStack.Pop();
                }
                else
                {
                    var parentIndex = parentIndexStack.Pop();
                    parentIndexes[indexInSymbols] = parentIndex;
                    if (parentIndex >= 0)
                    {
                        childrenCounts[parentIndex]++;
                    }
                    parentIndexStack.Push(indexInSymbols);
                }
            }

            //Traverse to find duplicated nesting symbols
            for(int nodeIndex = 0; nodeIndex < parentIndexes.Length; nodeIndex++)
            {
                var parentIndex = parentIndexes[nodeIndex];
                if(parentIndex < 0)
                {
                    // if parent is entry point, or no parent, nothing will happen to this node.
                    continue;
                }
                var currentSymbol = targetSymbolSeries[nodeIndex].targetSymbol;
                var parentSymbol = targetSymbolSeries[parentIndex].targetSymbol;
                if (parentSymbol == branchOpen && currentSymbol == branchOpen)
                {
                    // cut self out from underneath Parent, insert self under Grandparent
                    var grandparentIndex = parentIndexes[parentIndex];
                    parentIndexes[nodeIndex] = grandparentIndex;
                    if(grandparentIndex >= 0)
                    {
                        childrenCounts[grandparentIndex]++;
                    }
                    // decrement child count of parent, if below 0 orphan it.
                    childrenCounts[parentIndex]--;
                    if(childrenCounts[parentIndex] <= 0)
                    {
                        parentIndexes[parentIndex] = -2;
                    }
                }
            }

            var childIndexes = new SortedSet<int>[targetSymbolSeries.Length + 1];
            childIndexes[0] = new SortedSet<int>();
            for (int graphIndex = 1; graphIndex < childIndexes.Length; graphIndex++)
            {
                childIndexes[graphIndex] = new SortedSet<int>();
                var parentIndex = parentIndexes[graphIndex - 1] + 1;
                if(parentIndex >= 0)
                {
                    childIndexes[parentIndex].Add(graphIndex);
                }
            }

            graphPointers = childIndexes.Select(x => x.ToArray()).ToArray();
        }

        public static SymbolSeriesMatcher Parse(string symbolString)
        {
            return new SymbolSeriesMatcher
            {
                targetSymbolSeries = InputSymbolParser.ParseInputSymbols(symbolString)
            };
        }
    }
}
