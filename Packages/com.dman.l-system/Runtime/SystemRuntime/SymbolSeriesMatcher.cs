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
            var parentIndexes = new int[targetSymbolSeries.Length];
            var childIndexes = new SortedSet<int>[targetSymbolSeries.Length + 1];
            childIndexes[0] = new SortedSet<int>();
            var parentIndexStack = new Stack<int>();
            parentIndexStack.Push(0);
            for (int indexInGraph = 1; indexInGraph < childIndexes.Length; indexInGraph++)
            {
                childIndexes[indexInGraph] = new SortedSet<int>();

                var targetSymbol = targetSymbolSeries[indexInGraph - 1].targetSymbol;
                if(targetSymbol == branchOpen)
                {
                    childIndexes[parentIndexStack.Peek()].Add(indexInGraph);
                    parentIndexStack.Push(indexInGraph);
                }else if (targetSymbol == branchClose)
                {
                    parentIndexStack.Pop();
                }
                else
                {
                    var parentIndex = parentIndexStack.Pop();
                    childIndexes[parentIndex].Add(indexInGraph);
                    parentIndexStack.Push(indexInGraph);
                }
            }

            //var visitStack = new Stack<GraphVisitIndex>();
            //visitStack.Push(new GraphVisitIndex
            //{
            //    grandParentIndex = -1,
            //    parentIndex = -1,
            //    parentSymbol = -1,
            //    visitIndex = 0
            //});
            //// DFS traverse to find duplicated nesting symbols
            //while(visitStack.Count > 0)
            //{
            //    var current = visitStack.Pop();
            //    var currentSymbol = -1;
            //    if (current.visitIndex >= 1)
            //    {
            //        currentSymbol = targetSymbolSeries[current.visitIndex - 1].targetSymbol;
            //    }
            //    if(current.parentSymbol == branchOpen && currentSymbol == branchOpen)
            //    {
            //        // cut self out from underneath Parent, insert self under Grandparent
            //        childIndexes[current.parentIndex].Remove(current.visitIndex);
            //        //childIndexes[current.grandParentIndex]
            //        // reduce graph here
            //    }
            //    var nextVisits = childIndexes[current.visitIndex];
            //    foreach (var visit in nextVisits)
            //    {
            //        visitStack.Push(new GraphVisitIndex
            //        {
            //            grandParentIndex = current.parentIndex,
            //            parentIndex = current.visitIndex,
            //            parentSymbol = currentSymbol,
            //            visitIndex = visit
            //        });
            //    }
            //}

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
