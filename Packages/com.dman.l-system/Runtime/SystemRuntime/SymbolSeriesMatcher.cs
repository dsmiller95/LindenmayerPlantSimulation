using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{
    internal class SymbolSeriesMatcher
    {
        public InputSymbol[] targetSymbolSeries;

        /// <summary>
        /// -1 is valid, indicating the parent is the entry point to the graph.
        ///     -2 is invalid, and indicates a node with no parent.
        /// All other values will be indexes in <see cref="targetSymbolSeries"/>
        /// </summary>
        /// <param name="symbolIndex"></param>
        /// <returns></returns>
        public int ParentOf(int symbolIndex)
        {
            return graphParentPointers[symbolIndex];
        }

        public bool IsLeaf(int symbolIndex)
        {
            return graphChildPointers[symbolIndex + 1].Length <= 0;
        }

        /// <summary>
        /// Jagged array used to represent the branching structure of <see cref="targetSymbolSeries"/>. 
        /// First element is entry point into the symbols, and does not represent any symbol in itself.
        ///     This array is shifted behind targetSymbolSeries by one. meaning that <see cref="graphChildPointers"/>[1]
        ///     refers to symbol <see cref="targetSymbolSeries"/>[0]
        /// </summary>
        public int[][] graphChildPointers;
        public int[] childrenCounts;
        /// <summary> 
        /// Indexes of parents. -1 is valid, indicating the parent is the entry point to the graph.
        ///     -2 is invalid, and indicates a node with no parent.
        /// </summary>
        public int[] graphParentPointers;
        public void ComputeGraphIndexes(int branchOpen, int branchClose)
        {
            graphParentPointers = new int[targetSymbolSeries.Length];
            childrenCounts = new int[targetSymbolSeries.Length];
            var parentIndexStack = new Stack<int>();
            parentIndexStack.Push(-1);
            for (int indexInSymbols = 0; indexInSymbols < targetSymbolSeries.Length; indexInSymbols++)
            {
                var targetSymbol = targetSymbolSeries[indexInSymbols].targetSymbol;
                if (targetSymbol == branchOpen)
                {
                    var parentIndex = parentIndexStack.Peek();
                    graphParentPointers[indexInSymbols] = parentIndex;
                    if (parentIndex >= 0)
                    {
                        childrenCounts[parentIndex]++;
                    }
                    parentIndexStack.Push(indexInSymbols);
                }
                else if (targetSymbol == branchClose)
                {
                    graphParentPointers[indexInSymbols] = -2;
                    parentIndexStack.Pop();
                }
                else
                {
                    var parentIndex = parentIndexStack.Pop();
                    graphParentPointers[indexInSymbols] = parentIndex;
                    if (parentIndex >= 0)
                    {
                        childrenCounts[parentIndex]++;
                    }
                    parentIndexStack.Push(indexInSymbols);
                }
            }

            //Traverse to remove all nesting symbols
            for (int nodeIndex = 0; nodeIndex < graphParentPointers.Length; nodeIndex++)
            {
                var parentIndex = graphParentPointers[nodeIndex];
                if (parentIndex < 0)
                {
                    // if parent is entry point, aka no parent, nothing will happen to this node.
                    continue;
                }
                var parentSymbol = targetSymbolSeries[parentIndex].targetSymbol;
                if (parentSymbol == branchOpen)
                {
                    // cut self out from underneath Parent, insert self under Grandparent
                    var grandparentIndex = graphParentPointers[parentIndex];
                    graphParentPointers[nodeIndex] = grandparentIndex;
                    if (grandparentIndex >= 0)
                    {
                        childrenCounts[grandparentIndex]++;
                    }
                    // decrement child count of parent, if below 0 orphan it.
                    childrenCounts[parentIndex]--;
                    if (childrenCounts[parentIndex] <= 0)
                    {
                        graphParentPointers[parentIndex] = -2;
                    }
                }
            }

            var childIndexes = new SortedSet<int>[targetSymbolSeries.Length + 1];
            childIndexes[0] = new SortedSet<int>();
            for (int graphIndex = 1; graphIndex < childIndexes.Length; graphIndex++)
            {
                childIndexes[graphIndex] = new SortedSet<int>();
                var parentIndex = graphParentPointers[graphIndex - 1] + 1;
                if (parentIndex >= 0)
                {
                    childIndexes[parentIndex].Add(graphIndex);
                }
            }

            graphChildPointers = childIndexes.Select(x => x.ToArray()).ToArray();
        }

        public IEnumerable<int> GetDepthFirstEnumerator()
        {
            var currentState = GetImmutableDepthFirstIterationState();
            while (currentState.Next(out var nextState))
            {
                yield return nextState.currentIndex;
                currentState = nextState;
            }
        }

        public DepthFirstSearchState GetImmutableDepthFirstIterationState()
        {
            return new DepthFirstSearchState(this, -1, ImmutableStack<int>.Empty);
        }

        public struct DepthFirstSearchState
        {
            public int currentIndex { get; private set; }
            private SymbolSeriesMatcher source;
            private ImmutableStack<int> indexInAncestors;


            public DepthFirstSearchState(SymbolSeriesMatcher source, int currentIndex, ImmutableStack<int> indexInAncenstors)
            {
                this.source = source;
                this.currentIndex = currentIndex;
                indexInAncestors = indexInAncenstors;
            }

            public IEnumerable<DepthFirstSearchState> TakeNNext(int nextNum)
            {
                var stateTracker = this;
                for (int i = 0; i < nextNum; i++)
                {
                    if (!stateTracker.Next(out stateTracker))
                    {
                        yield break;
                    }
                    yield return stateTracker;
                }
            }
            public IEnumerable<DepthFirstSearchState> TakeNPrevious(int previousNum)
            {
                var stateTracker = this;
                for (int i = 0; i < previousNum; i++)
                {
                    if (!stateTracker.Previous(out stateTracker))
                    {
                        yield break;
                    }
                    yield return stateTracker;
                }
            }

            public bool Next(out DepthFirstSearchState nextState)
            {
                var nextIndex = currentIndex;
                var nextAncestorsStack = indexInAncestors;

                var children = source.graphChildPointers[nextIndex + 1];
                var indexInChildren = 0;


                while (indexInChildren >= children.Length && nextIndex >= 0)
                {
                    nextIndex = source.graphParentPointers[nextIndex];
                    children = source.graphChildPointers[nextIndex + 1];
                    nextAncestorsStack = nextAncestorsStack.Pop(out var lastIndexInChildren);
                    indexInChildren = lastIndexInChildren + 1;
                }
                if (indexInChildren < children.Length)
                {
                    nextAncestorsStack = nextAncestorsStack.Push(indexInChildren);
                    nextIndex = children[indexInChildren] - 1;
                    nextState = new DepthFirstSearchState(source, nextIndex, nextAncestorsStack);
                    return true;
                }
                nextState = default;
                return false;
            }
            public bool Previous(out DepthFirstSearchState previousState)
            {
                var nextNode = currentIndex;
                var nextAncestorsStack = indexInAncestors;
                if (nextAncestorsStack.IsEmpty)
                {
                    previousState = default;
                    return false;
                }

                nextAncestorsStack = nextAncestorsStack.Pop(out var currentChildIndex);
                currentChildIndex--;
                nextNode = source.graphParentPointers[nextNode];
                while (currentChildIndex >= 0)
                {
                    // descend down the right-hand-side of the tree
                    nextAncestorsStack = nextAncestorsStack.Push(currentChildIndex);
                    nextNode = source.graphChildPointers[nextNode + 1][currentChildIndex] - 1;
                    currentChildIndex = source.graphChildPointers[nextNode + 1].Length - 1;
                }
                if (currentChildIndex < 0)
                {
                    previousState = new DepthFirstSearchState(source, nextNode, nextAncestorsStack);
                    return true;
                }

                previousState = default;
                return false;
            }

            public bool FindPreviousWithParent(out DepthFirstSearchState foundState, int parentNode)
            {
                foundState = this;
                do
                {
                    var parent = source.graphParentPointers[foundState.currentIndex];
                    if (parent == parentNode)
                    {
                        return true;
                    }
                } while (foundState.Previous(out foundState) && foundState.currentIndex >= 0);
                return false;
            }

            public void Reset()
            {
                currentIndex = -1;
                indexInAncestors.Clear();
            }
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
