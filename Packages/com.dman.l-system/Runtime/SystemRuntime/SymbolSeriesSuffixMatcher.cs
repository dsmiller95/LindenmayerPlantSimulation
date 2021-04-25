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

        public InputSymbol.Blittable mySymbol;
    }

    public class SymbolSeriesPrefixBuilder
    {
        public InputSymbol[] targetSymbolSeries;

        public SymbolSeriesPrefixBuilder(InputSymbol[] matchingSeries)
        {
            this.targetSymbolSeries = matchingSeries;
        }
        public SymbolSeriesPrefixMatcher BuildIntoManagedMemory(Allocator allocator = Allocator.Persistent)
        {
            var matcher = new SymbolSeriesPrefixMatcher();

            matcher.targetSymbolSeries = new NativeArray<InputSymbol.Blittable>(
                targetSymbolSeries
                    .Select(x => x.AsBlittable())
                    .ToArray(),
                allocator);

            matcher.IsValid = targetSymbolSeries.Length > 0;

            return matcher;
        }
        public static SymbolSeriesPrefixBuilder Parse(string symbolString)
        {
            return new SymbolSeriesPrefixBuilder(InputSymbolParser.ParseInputSymbols(symbolString));
        }
    }

    public class SymbolSeriesSuffixBuilder
    {
        public InputSymbol[] targetSymbolSeries;
        
        public bool HasGraphIndexes { get; private set; }

        private List<SymbolMatcherNode> nodes;
        private SortedSet<int> rootChildren;
        public int RequiredChildrenMemSpace { get; private set; }
        public int RequiredGraphNodeMemSpace { get; private set; }

        class SymbolMatcherNode
        {
            public int parentIndex;
            public int myIndexInParentChildren;
            public SortedSet<int> childrenIndexes = new SortedSet<int>();
        }

        public SymbolSeriesSuffixBuilder(InputSymbol[] matchingSeries)
        {
            this.targetSymbolSeries = matchingSeries;
        }

        public void BuildGraphIndexes(int branchOpen, int branchClose)
        {
            nodes = new List<SymbolMatcherNode>(targetSymbolSeries.Length);

            // TODO: might not need children counts here. 
            var childrenCounts = new NativeArray<int>(targetSymbolSeries.Length, Allocator.Persistent);
            var parentIndexStack = new Stack<int>();
            parentIndexStack.Push(-1);
            for (int indexInSymbols = 0; indexInSymbols < targetSymbolSeries.Length; indexInSymbols++)
            {
                var targetSymbol = targetSymbolSeries[indexInSymbols].targetSymbol;
                var node = new SymbolMatcherNode();
                nodes.Add(node);
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
            }

            //Traverse to remove all nesting symbols
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
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

                    if (grandparentIndex >= 0)
                    {
                        childrenCounts[grandparentIndex]++;
                    }
                    // decrement child count of parent, if below 0 orphan it.
                    childrenCounts[parentIndex]--;
                    if (childrenCounts[parentIndex] <= 0)
                    {
                        parentNode.parentIndex = -2;
                    }
                }
            }
            childrenCounts.Dispose();

            rootChildren = new SortedSet<int>();
            for (int graphIndex = 0; graphIndex < nodes.Count; graphIndex++)
            {
                var node = nodes[graphIndex];
                var parentIndex = node.parentIndex;
                if (parentIndex >= 0)
                {
                    nodes[parentIndex].childrenIndexes.Add(graphIndex);
                }
                else if (parentIndex == -1)
                {
                    rootChildren.Add(graphIndex);
                }
            }


            if (rootChildren.Count != 0)
            {
                var childNum = 0;
                foreach (var childIndex in rootChildren)
                {
                    var childNode = nodes[childIndex];
                    childNode.myIndexInParentChildren = childNum;
                    childNum++;
                }
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var childNum = 0;
                foreach (var childIndex in node.childrenIndexes)
                {
                    var childNode = nodes[childIndex];
                    childNode.myIndexInParentChildren = childNum;
                    childNum++;
                }
            }
            HasGraphIndexes = true;

            RequiredChildrenMemSpace = rootChildren.Count + nodes.Sum(x => x.childrenIndexes.Count);
            RequiredGraphNodeMemSpace = nodes.Count;
        }

        public SymbolSeriesSuffixMatcher BuildIntoManagedMemory(
            SymbolSeriesMatcherNativeDataArray nativeData,
            SymbolSeriesMatcherNativeDataWriter dataWriter,
            Allocator allocator = Allocator.Persistent)
        {
            var matcher = new SymbolSeriesSuffixMatcher();

            matcher.graphNodeMemSpace = new JaggedIndexing
            {
                index = dataWriter.indexInGraphNode,
                length = (ushort)RequiredGraphNodeMemSpace
            };

            dataWriter.indexInGraphNode += RequiredGraphNodeMemSpace;
            for (int i = 0; i < nodes.Count; i++)
            {
                var sourceNode = nodes[i];
                nativeData.graphNodeData[i + matcher.graphNodeMemSpace.index] = new SymbolMatcherGraphNode
                {
                    parentIndex = sourceNode.parentIndex,
                    myIndexInParentChildren = sourceNode.myIndexInParentChildren,
                    mySymbol = targetSymbolSeries[i].AsBlittable()
                };
            }

            matcher.childrenOfRoot = new JaggedIndexing
            {
                index = dataWriter.indexInChildren,
                length = (ushort)rootChildren.Count
            };

            var childrenAsArray = nodes.Select(x => x.childrenIndexes.ToArray()).ToArray();
            var tmpIndexInChildren = 0;
            foreach (var rootChild in rootChildren)
            {
                nativeData.childrenDataArray[tmpIndexInChildren + dataWriter.indexInChildren] = rootChild;
                tmpIndexInChildren++;
            }
            JaggedNativeArray<int>.WriteJaggedIndexing(
                (indexInJagged, jaggedIndexing) =>
                {
                    var node = nativeData.graphNodeData[indexInJagged + matcher.graphNodeMemSpace.index];
                    node.childrenIndexing = jaggedIndexing;
                    nativeData.graphNodeData[indexInJagged + matcher.graphNodeMemSpace.index] = node;
                },
                childrenAsArray,
                nativeData.childrenDataArray,
                dataWriter.indexInChildren + tmpIndexInChildren
                );
            dataWriter.indexInChildren += RequiredChildrenMemSpace;

            matcher.HasGraphIndexes = true;
            matcher.IsCreated = true;

            return matcher;
        }
        public static SymbolSeriesSuffixBuilder Parse(string symbolString)
        {
            return new SymbolSeriesSuffixBuilder(InputSymbolParser.ParseInputSymbols(symbolString));
        }
    }


    public struct SymbolSeriesPrefixMatcher : IDisposable
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<InputSymbol.Blittable> targetSymbolSeries;
        public bool IsValid;
        public void Dispose()
        {
            this.targetSymbolSeries.Dispose();
        }
    }

    public struct SymbolSeriesSuffixMatcher: IDisposable
    {
        public JaggedIndexing childrenOfRoot;
        public JaggedIndexing graphNodeMemSpace;


        public bool IsCreated { get; set;  }
        public bool HasGraphIndexes { get; set; }

        public IEnumerable<int> GetDepthFirstEnumerator(SymbolSeriesMatcherNativeDataArray nativeData)
        {
            var currentState = GetImmutableDepthFirstIterationState(nativeData);
            while (currentState.Next(out var nextState))
            {
                yield return nextState.currentIndex;
                currentState = nextState;
            }
        }

        public DepthFirstSearchState GetImmutableDepthFirstIterationState(SymbolSeriesMatcherNativeDataArray nativeData)
        {
            return new DepthFirstSearchState(this, -1, nativeData);
        }

        private JaggedIndexing ChildrenForNode(int nodeIndex, SymbolSeriesMatcherNativeDataArray nativeData)
        {
            if (nodeIndex >= 0)
            {
                var node = nativeData.graphNodeData[nodeIndex + graphNodeMemSpace.index];
                return node.childrenIndexing;
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
            private SymbolSeriesSuffixMatcher source;
            // Should be OK to store a pointer here. this cannot be copied over, but it is only used
            //  during job execution
            private SymbolSeriesMatcherNativeDataArray nativeData;

            public DepthFirstSearchState(
                SymbolSeriesSuffixMatcher source,
                int currentIndex,
                SymbolSeriesMatcherNativeDataArray nativeDataPointer)
            {
                this.source = source;
                this.currentIndex = currentIndex;
                this.nativeData = nativeDataPointer;
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
                var children = source.ChildrenForNode(nextIndex, nativeData);


                while (indexInChildren >= children.length && nextIndex >= 0)
                {
                    var currentNode = nativeData.graphNodeData[nextIndex + source.graphNodeMemSpace.index];
                    var lastIndexInChildren = currentNode.myIndexInParentChildren;

                    nextIndex = currentNode.parentIndex;
                    children = source.ChildrenForNode(nextIndex, nativeData);
                    indexInChildren = lastIndexInChildren + 1;
                }
                if (indexInChildren < children.length)
                {
                    nextIndex = nativeData.childrenDataArray[indexInChildren + children.index];
                    nextState = new DepthFirstSearchState(source, nextIndex, nativeData);
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

                var currentChildIndex = nativeData.graphNodeData[nextNodeIndex + source.graphNodeMemSpace.index].myIndexInParentChildren;
                currentChildIndex--;
                nextNodeIndex = nativeData.graphNodeData[nextNodeIndex + source.graphNodeMemSpace.index].parentIndex;
                while (currentChildIndex >= 0)
                {
                    // descend down the right-hand-side of the tree
                    var childrenIndexing = source.ChildrenForNode(nextNodeIndex, nativeData);
                    nextNodeIndex = nativeData.childrenDataArray[childrenIndexing.index + currentChildIndex];
                    //nextNode = source.nodes[nextNodeIndex];
                    childrenIndexing = source.ChildrenForNode(nextNodeIndex, nativeData);
                    currentChildIndex = childrenIndexing.length - 1;
                }
                if (currentChildIndex < 0)
                {
                    previousState = new DepthFirstSearchState(source, nextNodeIndex, nativeData);
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
                    var parent = nativeData.graphNodeData[foundState.currentIndex + source.graphNodeMemSpace.index].parentIndex;
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

        public void Dispose()
        {
        }

    }
}
