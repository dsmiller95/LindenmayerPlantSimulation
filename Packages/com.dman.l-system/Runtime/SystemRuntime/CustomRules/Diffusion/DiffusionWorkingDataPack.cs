using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules.Diffusion
{

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

        public CustomRuleSymbols customSymbols;
        public bool IsCreated;

        public DiffusionWorkingDataPack(int estimatedEdges, int estimatedNodes, int estimatedUniqueResources, CustomRuleSymbols customSymbols, Allocator allocator = Allocator.TempJob)
        {
            this.customSymbols = customSymbols;
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            allEdges = new NativeList<DiffusionEdge>(estimatedEdges, allocator);
            nodes = new NativeList<DiffusionNode>(estimatedNodes, allocator);
            UnityEngine.Profiling.Profiler.EndSample();

            nodeMaxCapacities = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
            nodeAmountsListA = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
            nodeAmountsListB = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
            IsCreated = true;
        }

        public void PerformDiffusionOnDataAndApply(SymbolString<float> targetSymbols)
        {
            var latestDataInA = true;
            for (int i = 0; i < customSymbols.diffusionStepsPerStep; i++)
            {
                DiffuseBetween(latestDataInA);
                latestDataInA = !latestDataInA;
            }
            ApplyDiffusionResults(latestDataInA, targetSymbols);
        }
        private void DiffuseBetween(bool diffuseAtoB)
        {
            var sourceDiffuseAmounts = diffuseAtoB ? nodeAmountsListA : nodeAmountsListB;
            var targetDiffuseAmounts = diffuseAtoB ? nodeAmountsListB : nodeAmountsListA;

            targetDiffuseAmounts.CopyFrom(sourceDiffuseAmounts);

            for (int edgeIndex = 0; edgeIndex < allEdges.Length; edgeIndex++)
            {
                var edge = allEdges[edgeIndex];
                DiffuseAcrossEdge(edge, sourceDiffuseAmounts, targetDiffuseAmounts);
            }
        }


        private void DiffuseAcrossEdge(
            DiffusionEdge edge,
            NativeList<float> sourceAmounts,
            NativeList<float> targetAmounts
            )
        {
            var nodeA = nodes[edge.nodeAIndex];
            var nodeB = nodes[edge.nodeBIndex];

            var diffusionConstant = customSymbols.diffusionConstantRuntimeGlobalMultiplier * (nodeA.diffusionConstant + nodeB.diffusionConstant) / 2f;
            for (
                int resource = 0;
                resource < nodeA.totalResourceTypes && resource < nodeA.totalResourceTypes;
                resource++)
            {
                var oldNodeAValue = sourceAmounts[nodeA.indexInTempAmountList + resource];
                var nodeAValueCap = nodeMaxCapacities[nodeA.indexInTempAmountList + resource];

                var oldNodeBValue = sourceAmounts[nodeB.indexInTempAmountList + resource];
                var nodeBValueCap = nodeMaxCapacities[nodeB.indexInTempAmountList + resource];

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

        private void ApplyDiffusionResults(bool latestDataIsInA, SymbolString<float> targetSymbols)
        {
            var amountData = latestDataIsInA ? nodeAmountsListA : nodeAmountsListB;
            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                var node = nodes[nodeIndex];
                targetSymbols[node.indexInTarget] = customSymbols.diffusionNode;

                targetSymbols.parameters[node.indexInTarget] = node.targetParameters;

                targetSymbols.parameters[node.targetParameters, 0] = node.diffusionConstant;
                for (int resourceType = 0; resourceType < node.totalResourceTypes; resourceType++)
                {
                    targetSymbols.parameters[node.targetParameters, resourceType * 2 + 1] = amountData[node.indexInTempAmountList + resourceType];
                    targetSymbols.parameters[node.targetParameters, resourceType * 2 + 2] = nodeMaxCapacities[node.indexInTempAmountList + resourceType];
                }
            }
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
