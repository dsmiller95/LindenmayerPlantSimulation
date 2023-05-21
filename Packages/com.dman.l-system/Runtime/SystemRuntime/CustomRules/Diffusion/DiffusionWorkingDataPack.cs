#if !RUST_SUBSYSTEM
using Dman.LSystem.Extern;
using Dman.LSystem.Extern.Adapters;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules.Diffusion
{
    internal struct DiffusionWorkingDataPack : INativeDisposable
    {
        public struct DiffusionEdge
        {
            public int node_a_index;
            public int node_b_index;
        }

        public struct DiffusionNode
        {
            public int index_in_target;
            public JaggedIndexing target_parameters;
            public int index_in_temp_amount_list;
            public int total_resource_types;
            public float diffusion_constant;
        }

        [NativeDisableParallelForRestriction] public NativeList<DiffusionEdge> allEdges;
        [NativeDisableParallelForRestriction] public NativeList<DiffusionNode> nodes;

        [NativeDisableParallelForRestriction] public NativeList<float> nodeMaxCapacities;
        [NativeDisableParallelForRestriction] public NativeList<float> nodeAmountsListA;
        [NativeDisableParallelForRestriction] public NativeList<float> nodeAmountsListB;

        public CustomRuleSymbols customSymbols;
        public bool IsCreated;

        public DiffusionWorkingDataPack(
            int estimatedEdges,
            int estimatedNodes,
            int estimatedUniqueResources,
            CustomRuleSymbols customSymbols,
            Allocator allocator = Allocator.TempJob)
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
            // ReSharper disable once JoinDeclarationAndInitializer
            bool latestDataInA;

            latestDataInA = true;
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
            var nodeA = nodes[edge.node_a_index];
            var nodeB = nodes[edge.node_b_index];

            var diffusionConstant = customSymbols.diffusionConstantRuntimeGlobalMultiplier *
                (nodeA.diffusion_constant + nodeB.diffusion_constant) / 2f;
            for (
                int resource = 0;
                resource < nodeA.total_resource_types && resource < nodeB.total_resource_types;
                resource++)
            {
                var oldNodeAValue = sourceAmounts[nodeA.index_in_temp_amount_list + resource];
                var nodeAValueCap = nodeMaxCapacities[nodeA.index_in_temp_amount_list + resource];

                var oldNodeBValue = sourceAmounts[nodeB.index_in_temp_amount_list + resource];
                var nodeBValueCap = nodeMaxCapacities[nodeB.index_in_temp_amount_list + resource];

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

                targetAmounts[nodeA.index_in_temp_amount_list + resource] += aToBTransferredAmount;
                targetAmounts[nodeB.index_in_temp_amount_list + resource] -= aToBTransferredAmount;
            }
        }

        private void ApplyDiffusionResults(bool latestDataIsInA, SymbolString<float> targetSymbols)
        {
            var amountData = latestDataIsInA ? nodeAmountsListA : nodeAmountsListB;
            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                var node = nodes[nodeIndex];
                targetSymbols[node.index_in_target] = customSymbols.diffusionNode;

                targetSymbols.parameters[node.index_in_target] = node.target_parameters;

                targetSymbols.parameters[node.target_parameters, 0] = node.diffusion_constant;
                for (int resourceType = 0; resourceType < node.total_resource_types; resourceType++)
                {
                    targetSymbols.parameters[node.target_parameters, resourceType * 2 + 1] =
                        amountData[node.index_in_temp_amount_list + resourceType];
                    targetSymbols.parameters[node.target_parameters, resourceType * 2 + 2] =
                        nodeMaxCapacities[node.index_in_temp_amount_list + resourceType];
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
#endif