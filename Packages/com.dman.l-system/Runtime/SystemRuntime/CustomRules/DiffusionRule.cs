using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    [BurstCompile]
    struct DiffusionReplacementJob : IJob
    {
        [ReadOnly]
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public SymbolString<float> sourceData;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction] // disable all safety to allow parallel writes
        public SymbolString<float> targetData;

        [ReadOnly]
        public SymbolStringBranchingCache branchingCache;

        internal DiffusionWorkingDataPack working;

        public CustomRuleSymbols customSymbols;


        public void Execute()
        {
            if (customSymbols.hasDiffusion)
            {
                ExtractEdgesAndNodes();

                var latestDataInA = true;
                for (int i = 0; i < customSymbols.diffusionStepsPerStep; i++)
                {
                    DiffuseBetween(latestDataInA);
                    latestDataInA = !latestDataInA;
                }

                ApplyDiffusionResults(latestDataInA);
            }
        }

        private void ExtractEdgesAndNodes()
        {
            var branchSymbolParentStack = new TmpNativeStack<BranchEvent>(5);
            var currentNodeParent = -1;

            for (int symbolIndex = 0; symbolIndex < sourceData.Length; symbolIndex++)
            {
                var symbol = sourceData[symbolIndex];
                if (symbol == customSymbols.diffusionNode)
                {
                    if (currentNodeParent >= 0)
                    {
                        var newEdge = new DiffusionEdge
                        {
                            nodeAIndex = currentNodeParent,
                            nodeBIndex = working.nodes.Length
                        };
                        working.allEdges.Add(newEdge);
                    }
                    currentNodeParent = working.nodes.Length;

                    var nodeParams = sourceData.parameters[symbolIndex];
                    var nodeSingleton = matchSingletonData[symbolIndex];

                    var newNode = new DiffusionNode
                    {
                        indexInTarget = nodeSingleton.replacementSymbolIndexing.index,
                        targetParameters = nodeSingleton.replacementParameterIndexing,

                        indexInTempAmountList = working.nodeAmountsListA.Length,

                        totalResourceTypes = (nodeParams.length - 1) / 2,
                        diffusionConstant = sourceData.parameters[nodeParams, 0],
                    };
                    newNode.targetParameters.length = nodeParams.length;
                    working.nodes.Add(newNode);

                    for (int resourceType = 0; resourceType < newNode.totalResourceTypes; resourceType++)
                    {
                        var currentAmount = sourceData.parameters[nodeParams, resourceType * 2 + 1];
                        var maxCapacity = sourceData.parameters[nodeParams, resourceType * 2 + 1 + 1];
                        working.nodeAmountsListA.Add(currentAmount);
                        working.nodeAmountsListB.Add(0);
                        working.nodeMaxCapacities.Add(maxCapacity);
                    }

                }
                else if (symbol == customSymbols.diffusionAmount)
                {
                    if (currentNodeParent < 0)
                    {
                        // problem: the amount will dissapear
                        continue;
                    }
                    var modifiedNode = working.nodes[currentNodeParent];
                    var amountParameters = sourceData.parameters[symbolIndex];
                    for (int resourceType = 0; resourceType < modifiedNode.totalResourceTypes && resourceType < amountParameters.length; resourceType++)
                    {
                        working.nodeAmountsListA[modifiedNode.indexInTempAmountList + resourceType] += sourceData.parameters[amountParameters, resourceType];
                    }
                }
                else if (symbol == branchingCache.branchOpenSymbol)
                {
                    branchSymbolParentStack.Push(new BranchEvent
                    {
                        openBranchSymbolIndex = symbolIndex,
                        currentNodeParent = currentNodeParent
                    });
                }
                else if (symbol == branchingCache.branchCloseSymbol)
                {
                    var lastBranchState = branchSymbolParentStack.Pop();
                    currentNodeParent = lastBranchState.currentNodeParent;
                }
            }
        }

        struct BranchEvent
        {
            public int openBranchSymbolIndex;
            public int currentNodeParent;
        }

        private void DiffuseBetween(bool diffuseAtoB)
        {
            var sourceDiffuseAmounts = diffuseAtoB ? working.nodeAmountsListA : working.nodeAmountsListB;
            var targetDiffuseAmounts = diffuseAtoB ? working.nodeAmountsListB : working.nodeAmountsListA;

            targetDiffuseAmounts.CopyFrom(sourceDiffuseAmounts);

            for (int edgeIndex = 0; edgeIndex < working.allEdges.Length; edgeIndex++)
            {
                var edge = working.allEdges[edgeIndex];
                DiffuseAcrossEdge(edge, sourceDiffuseAmounts, targetDiffuseAmounts);
            }
        }

        private void DiffuseAcrossEdge(
            DiffusionEdge edge,
            NativeList<float> sourceAmounts,
            NativeList<float> targetAmounts
            )
        {
            var nodeA = working.nodes[edge.nodeAIndex];
            var nodeB = working.nodes[edge.nodeBIndex];

            var diffusionConstant = (nodeA.diffusionConstant + nodeB.diffusionConstant) / 2f;
            for (
                int resource = 0;
                resource < nodeA.totalResourceTypes && resource < nodeA.totalResourceTypes;
                resource++)
            {
                var oldNodeAValue = sourceAmounts[nodeA.indexInTempAmountList + resource];
                var nodeAValueCap = working.nodeMaxCapacities[nodeA.indexInTempAmountList + resource];

                var oldNodeBValue = sourceAmounts[nodeB.indexInTempAmountList + resource];
                var nodeBValueCap = working.nodeMaxCapacities[nodeB.indexInTempAmountList + resource];

                var aToBTransferredAmount = diffusionConstant * (oldNodeBValue - oldNodeAValue);

                if (aToBTransferredAmount == 0)
                {
                    continue;
                }
                if (aToBTransferredAmount < 0 && oldNodeBValue >= nodeBValueCap)
                {
                    // the direction of flow is towards node B, and also node B is above its value cap. skip updating the resource on this connection completely.
                    continue;
                }
                if (aToBTransferredAmount > 0 && oldNodeAValue >= nodeAValueCap)
                {
                    // the direction of flow is towards node A, and also node A is above its value cap. skip updating the resource on this connection completely.
                    continue;
                }

                targetAmounts[nodeA.indexInTempAmountList + resource] += aToBTransferredAmount;
                targetAmounts[nodeB.indexInTempAmountList + resource] -= aToBTransferredAmount;
            }
        }

        private void ApplyDiffusionResults(bool latestDataIsInA)
        {
            var amountData = latestDataIsInA ? working.nodeAmountsListA : working.nodeAmountsListB;
            for (int nodeIndex = 0; nodeIndex < working.nodes.Length; nodeIndex++)
            {
                var node = working.nodes[nodeIndex];
                targetData[node.indexInTarget] = customSymbols.diffusionNode;

                targetData.parameters[node.indexInTarget] = node.targetParameters;

                targetData.parameters[node.targetParameters, 0] = node.diffusionConstant;
                for (int resourceType = 0; resourceType < node.totalResourceTypes; resourceType++)
                {
                    targetData.parameters[node.targetParameters, resourceType * 2 + 1] = amountData[node.indexInTempAmountList + resourceType];
                    targetData.parameters[node.targetParameters, resourceType * 2 + 2] = working.nodeMaxCapacities[node.indexInTempAmountList + resourceType];
                }
            }
        }

        internal struct DiffusionEdge
        {
            public int nodeAIndex;
            public int nodeBIndex;
        }

        internal struct DiffusionNode
        {
            public int indexInTarget;
            public JaggedIndexing targetParameters;

            public int indexInTempAmountList;

            public int totalResourceTypes;
            public float diffusionConstant;
        }


        internal struct DiffusionWorkingDataPack : INativeDisposable
        {
            [NativeDisableParallelForRestriction]
            public NativeList<DiffusionEdge> allEdges;
            [NativeDisableParallelForRestriction]
            public NativeList<DiffusionNode> nodes;

            [NativeDisableParallelForRestriction]
            public NativeList<float> nodeMaxCapacities;
            [NativeDisableParallelForRestriction]
            public NativeList<float> nodeAmountsListA;
            [NativeDisableParallelForRestriction]
            public NativeList<float> nodeAmountsListB;

            public DiffusionWorkingDataPack(int estimatedEdges, int estimatedNodes, int estimatedUniqueResources, Allocator allocator = Allocator.TempJob)
            {
                allEdges = new NativeList<DiffusionEdge>(estimatedEdges, allocator);
                nodes = new NativeList<DiffusionNode>(estimatedNodes, allocator);

                nodeMaxCapacities = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
                nodeAmountsListA = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
                nodeAmountsListB = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
            }

            public JobHandle Dispose(JobHandle inputDeps)
            {
                return JobHandle.CombineDependencies(
                    JobHandle.CombineDependencies(
                        allEdges.Dispose(inputDeps),
                        nodes.Dispose(inputDeps)
                    ),
                    JobHandle.CombineDependencies(
                        nodeMaxCapacities.Dispose(inputDeps),
                        nodeAmountsListA.Dispose(inputDeps),
                        nodeAmountsListB.Dispose(inputDeps)
                    ));
            }

            public void Dispose()
            {
                allEdges.Dispose();
                nodes.Dispose();
                nodeMaxCapacities.Dispose();
                nodeAmountsListA.Dispose();
                nodeAmountsListB.Dispose();
            }
        }
    }

}
