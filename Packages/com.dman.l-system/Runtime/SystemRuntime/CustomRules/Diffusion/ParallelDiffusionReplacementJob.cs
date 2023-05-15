﻿using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules.Diffusion
{
    /// <summary>
    /// this job can run in parallel with the rule replacement job. It will write to target data
    ///     in a way that the rule replacement job is designed to avoid conflict with
    /// </summary>
    [BurstCompile]
    struct ParallelDiffusionReplacementJob : IJob
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

        internal DiffusionWorkingDataPack working;

        public CustomRuleSymbols customSymbols;


        public void Execute()
        {
            if (customSymbols.hasDiffusion && !customSymbols.independentDiffusionUpdate)
            {
                ExtractEdgesAndNodes();

                working.PerformDiffusionOnDataAndApply(targetData);
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
                            node_a_index = currentNodeParent,
                            node_b_index = working.nodes.Length
                        };
                        working.allEdges.Add(newEdge);
                    }
                    currentNodeParent = working.nodes.Length;

                    var nodeParams = sourceData.parameters[symbolIndex];
                    var nodeSingleton = matchSingletonData[symbolIndex];

                    var newNode = new DiffusionNode
                    {
                        index_in_target = nodeSingleton.replacementSymbolIndexing.index,
                        target_parameters = nodeSingleton.replacementParameterIndexing,

                        index_in_temp_amount_list = working.nodeAmountsListA.Length,

                        total_resource_types = (nodeParams.length - 1) / 2,
                        diffusion_constant = sourceData.parameters[nodeParams, 0],
                    };
                    newNode.target_parameters.length = nodeParams.length;
                    working.nodes.Add(newNode);

                    for (int resourceType = 0; resourceType < newNode.total_resource_types; resourceType++)
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
                    var modifiedNode = working.nodes[currentNodeParent];
                    var amountParameters = sourceData.parameters[symbolIndex];
                    if (amountParameters.length == 0)
                    {
                        // the amount has no parameters left. removal will be happening via regular update
                        continue;
                    }

                    // clear out the parameters in the target string, and write the symbol over
                    var nodeSingleton = matchSingletonData[symbolIndex];
                    targetData.parameters[nodeSingleton.replacementSymbolIndexing.index] = new JaggedIndexing
                    {
                        index = nodeSingleton.replacementParameterIndexing.index,
                        length = 0
                    };
                    targetData[nodeSingleton.replacementSymbolIndexing.index] = customSymbols.diffusionAmount;
                    if (currentNodeParent < 0)
                    {
                        // problem: the amount will dissapear
                        continue;
                    }
                    for (int resourceType = 0; resourceType < modifiedNode.total_resource_types && resourceType < amountParameters.length; resourceType++)
                    {
                        working.nodeAmountsListA[modifiedNode.index_in_temp_amount_list + resourceType] += sourceData.parameters[amountParameters, resourceType];
                    }
                }
                else if (symbol == customSymbols.branchOpenSymbol)
                {
                    branchSymbolParentStack.Push(new BranchEvent
                    {
                        openBranchSymbolIndex = symbolIndex,
                        currentNodeParent = currentNodeParent
                    });
                }
                else if (symbol == customSymbols.branchCloseSymbol)
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

    }

}
