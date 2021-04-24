using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct SymbolMatcherGraphNode
    {
        /// <summary> 
        /// Indexes of parent. -1 is valid, indicating the parent is the entry point to the graph.
        ///     -2 is invalid, and indicates a node with no parent.
        /// </summary>
        public int parentIndex;
        /// <summary> 
        /// when this node has a parent, this is the position inside the parent's children array in which
        ///     the node at that index resides
        /// Values are undefined at indexes which have no parent (graphParentPointers[i] == -2)
        /// </summary>
        public int myIndexInParentChildren;
        /// <summary>
        /// Indexing into the children data array, representing the children that this node has.
        /// </summary>
        public JaggedIndexing childrenIndexing;
    }

    public struct SymbolSeriesMatcher: IDisposable
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<InputSymbol.Blittable> targetSymbolSeries;

        public SymbolSeriesMatcher(InputSymbol[] matchingSeries)
        {
            this.targetSymbolSeries = new NativeArray<InputSymbol.Blittable>(
                matchingSeries
                    .Select(x => x.AsBlittable())
                    .ToArray(),
                Allocator.Persistent);
            IsCreated = true;
            HasGraphIndexes = false;

            childrenDataArray = default;
            childrenOfRoot = default;
            nodes = default;
        }

        public NativeArray<int> childrenDataArray;
        public JaggedIndexing childrenOfRoot;
        public NativeArray<SymbolMatcherGraphNode> nodes;


        public bool IsCreated { get; private set; }
        public bool HasGraphIndexes { get; private set; }

        public void ComputeGraphIndexes(int branchOpen, int branchClose)
        {
            nodes = new NativeArray<SymbolMatcherGraphNode>(targetSymbolSeries.Length, Allocator.Persistent);

            // TODO: might not need children counts here. 
            var childrenCounts = new NativeArray<int>(targetSymbolSeries.Length, Allocator.Persistent);
            var parentIndexStack = new Stack<int>();
            parentIndexStack.Push(-1);
            for (int indexInSymbols = 0; indexInSymbols < targetSymbolSeries.Length; indexInSymbols++)
            {
                var targetSymbol = targetSymbolSeries[indexInSymbols].targetSymbol;
                var node = nodes[indexInSymbols];
                if (targetSymbol == branchOpen)
                {
                    var parentIndex = parentIndexStack.Peek();
                    node.parentIndex = parentIndex;
                    if (parentIndex >= 0)
                    {
                        childrenCounts[parentIndex]++;
                    }
                    parentIndexStack.Push(indexInSymbols);
                }
                else if (targetSymbol == branchClose)
                {
                    node.parentIndex = -2;
                    parentIndexStack.Pop();
                }
                else
                {
                    var parentIndex = parentIndexStack.Pop();
                    node.parentIndex = parentIndex;
                    if (parentIndex >= 0)
                    {
                        childrenCounts[parentIndex]++;
                    }
                    parentIndexStack.Push(indexInSymbols);
                }
                nodes[indexInSymbols] = node;
            }

            //Traverse to remove all nesting symbols
            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                var node = nodes[nodeIndex];
                var parentIndex = node.parentIndex;
                if (parentIndex < 0)
                {
                    // if parent is entry point, aka no parent, nothing will happen to this node.
                    continue;
                }
                var parentSymbol = targetSymbolSeries[parentIndex].targetSymbol;
                if (parentSymbol == branchOpen)
                {
                    // cut self out from underneath Parent, insert self under Grandparent
                    var parentNode = nodes[parentIndex];
                    var grandparentIndex = parentNode.parentIndex;
                    node.parentIndex = grandparentIndex;
                    nodes[nodeIndex] = node;

                    if (grandparentIndex >= 0)
                    {
                        childrenCounts[grandparentIndex]++;
                    }
                    // decrement child count of parent, if below 0 orphan it.
                    childrenCounts[parentIndex]--;
                    if (childrenCounts[parentIndex] <= 0)
                    {
                        parentNode.parentIndex = -2;
                        nodes[parentIndex] = parentNode;
                    }
                }
            }
            childrenCounts.Dispose();

            var childIndexes = new SortedSet<int>[targetSymbolSeries.Length];
            var rootChildren = new SortedSet<int>();
            for (int graphIndex = 0; graphIndex < childIndexes.Length; graphIndex++)
            {
                childIndexes[graphIndex] = new SortedSet<int>();
                var parentIndex = nodes[graphIndex].parentIndex;
                if (parentIndex >= 0)
                {
                    childIndexes[parentIndex].Add(graphIndex);
                }else if (parentIndex == -1)
                {
                    rootChildren.Add(graphIndex);
                }
            }

            var childrenAsArray = childIndexes.Select(x => x.ToArray()).ToArray();
            var rootChildrenCount = rootChildren.Count;
            var otherChildrenCount = childrenAsArray.Sum(x => x.Length);
            childrenDataArray = new NativeArray<int>(rootChildrenCount + otherChildrenCount, Allocator.Persistent);

            var indexInChildrenArray = 0;
            foreach (var rootChild in rootChildren)
            {
                childrenDataArray[indexInChildrenArray] = rootChild;
                indexInChildrenArray++;
            }
            childrenOfRoot = new JaggedIndexing
            {
                index = 0,
                length = (ushort)rootChildrenCount
            };
            JaggedNativeArray<int>.WriteJaggedIndexing(
                nodes,
                (node, indexing) =>
                {
                    node.childrenIndexing = indexing;
                    return node;
                },
                childrenAsArray,
                childrenDataArray,
                indexInChildrenArray
                );

            //graphChildPointers = new JaggedNativeArray<int>(childIndexes.Select(x => x.ToArray()).ToArray(), Allocator.Persistent);

            if(childrenOfRoot.length != 0)
            {
                for (int child = 0; child < childrenOfRoot.length; child++)
                {
                    var childIndex = childrenDataArray[child + childrenOfRoot.index];
                    var childNode = nodes[childIndex];
                    childNode.myIndexInParentChildren = child;
                    nodes[childIndex] = childNode;
                }
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if(node.childrenIndexing.length > 0)
                {
                    for (int child = 0; child < node.childrenIndexing.length; child++)
                    {
                        var childIndex = childrenDataArray[child + node.childrenIndexing.index];
                        var childNode = nodes[childIndex];
                        childNode.myIndexInParentChildren = child;
                        nodes[childIndex] = childNode;
                    }
                }
            }
            HasGraphIndexes = true;
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
            return new DepthFirstSearchState(this, -1);
        }

        private JaggedIndexing ChildrenForNode(int nodeIndex)
        {
            if (nodeIndex >= 0)
            {
                return nodes[nodeIndex].childrenIndexing;
            }
            else if (nodeIndex == -1)
            {
                return childrenOfRoot;
            }
            throw new Exception("invalid node index: " + nodeIndex);
        }

        public struct DepthFirstSearchState
        {
            public int currentIndex { get; private set; }
            private SymbolSeriesMatcher source;

            public DepthFirstSearchState(SymbolSeriesMatcher source, int currentIndex)
            {
                this.source = source;
                this.currentIndex = currentIndex;
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
                var indexInChildren = 0;
                var children = source.ChildrenForNode(nextIndex);


                while (indexInChildren >= children.length && nextIndex >= 0)
                {
                    var currentNode = source.nodes[nextIndex];
                    var lastIndexInChildren = currentNode.myIndexInParentChildren;

                    nextIndex = currentNode.parentIndex;
                    children = source.ChildrenForNode(nextIndex);
                    indexInChildren = lastIndexInChildren + 1;
                }
                if (indexInChildren < children.length)
                {
                    nextIndex = source.childrenDataArray[children.index + indexInChildren];
                    nextState = new DepthFirstSearchState(source, nextIndex);
                    return true;
                }
                nextState = default;
                return false;
            }

            public bool Previous(out DepthFirstSearchState previousState)
            {
                var nextNodeIndex = currentIndex;
                if (nextNodeIndex < 0)
                {
                    previousState = default;
                    return false;
                }

                var currentChildIndex = source.nodes[nextNodeIndex].myIndexInParentChildren;
                currentChildIndex--;
                nextNodeIndex = source.nodes[nextNodeIndex].parentIndex;
                while (currentChildIndex >= 0)
                {
                    // descend down the right-hand-side of the tree
                    var childrenIndexing = source.ChildrenForNode(nextNodeIndex);
                    nextNodeIndex = source.childrenDataArray[childrenIndexing.index + currentChildIndex];
                    //nextNode = source.nodes[nextNodeIndex];
                    childrenIndexing = source.ChildrenForNode(nextNodeIndex);
                    currentChildIndex = childrenIndexing.length - 1;
                }
                if (currentChildIndex < 0)
                {
                    previousState = new DepthFirstSearchState(source, nextNodeIndex);
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
                    var parent = source.nodes[foundState.currentIndex].parentIndex;
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
            }
        }

        public static SymbolSeriesMatcher Parse(string symbolString)
        {
            return new SymbolSeriesMatcher(InputSymbolParser.ParseInputSymbols(symbolString));
        }
        public void Dispose()
        {
            this.targetSymbolSeries.Dispose();
            this.childrenDataArray.Dispose();
            this.nodes.Dispose();
        }

    }
}
