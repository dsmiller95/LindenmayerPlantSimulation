using Dman.LSystem.Extern;
using Dman.LSystem.Extern.Adapters;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules.Diffusion
{
    /// <summary>
    /// this job runs after the rule replacement job, operating on the symbols in-place
    /// </summary>
    [BurstCompile]
    struct IndependentDiffusionReplacementJob : IJob
    {
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction] // disable all safety to allow parallel writes
        public SymbolString<float> inPlaceSymbols;

#if !RUST_SUBSYSTEM
        internal DiffusionWorkingDataPack working;
#endif

        public CustomRuleSymbols customSymbols;


        public void Execute()
        {
            if (customSymbols.hasDiffusion && customSymbols.independentDiffusionUpdate)
            {
#if RUST_SUBSYSTEM
                NativeDiffusion.InPlaceDiffusion(
                    Interop.FromMut(inPlaceSymbols),
                    customSymbols.diffusionNode,
                    customSymbols.diffusionAmount,
                    customSymbols.branchOpenSymbol,
                    customSymbols.branchCloseSymbol,
                    customSymbols.diffusionStepsPerStep,
                    customSymbols.diffusionConstantRuntimeGlobalMultiplier
                );
#else
                ExtractEdgesAndNodes();
                working.PerformDiffusionOnDataAndApply(inPlaceSymbols);
#endif
            }
        }

#if !RUST_SUBSYSTEM
        private void ExtractEdgesAndNodes()
        {
            var branchSymbolParentStack = new TmpNativeStack<BranchEvent>(5);
            var currentNodeParent = -1;

            for (int symbolIndex = 0; symbolIndex < inPlaceSymbols.Length; symbolIndex++)
            {
                var symbol = inPlaceSymbols[symbolIndex];
                if (symbol == customSymbols.diffusionNode)
                {
                    if (currentNodeParent >= 0)
                    {
                        var newEdge = new DiffusionWorkingDataPack.DiffusionEdge
                        {
                            node_a_index = currentNodeParent,
                            node_b_index = working.nodes.Length
                        };
                        working.allEdges.Add(newEdge);
                    }
                    currentNodeParent = working.nodes.Length;

                    var nodeParams = inPlaceSymbols.parameters[symbolIndex];

                    var newNode = new DiffusionWorkingDataPack.DiffusionNode
                    {
                        index_in_target = symbolIndex,
                        target_parameters = nodeParams,

                        index_in_temp_amount_list = working.nodeAmountsListA.Length,

                        total_resource_types = (nodeParams.length - 1) / 2,
                        diffusion_constant = inPlaceSymbols.parameters[nodeParams, 0],
                    };
                    newNode.target_parameters.length = nodeParams.length;
                    working.nodes.Add(newNode);

                    for (int resourceType = 0; resourceType < newNode.total_resource_types; resourceType++)
                    {
                        var currentAmount = inPlaceSymbols.parameters[nodeParams, resourceType * 2 + 1];
                        var maxCapacity = inPlaceSymbols.parameters[nodeParams, resourceType * 2 + 1 + 1];
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
                    var amountParameters = inPlaceSymbols.parameters[symbolIndex];
                    if (amountParameters.length == 0)
                    {
                        // the amount has no parameters left. removal will be happening via regular update
                        continue;
                    }
                    inPlaceSymbols.parameters[symbolIndex] = new JaggedIndexing
                    {
                        index = amountParameters.index,
                        length = 0
                    };
                    for (int resourceType = 0; resourceType < modifiedNode.total_resource_types && resourceType < amountParameters.length; resourceType++)
                    {
                        working.nodeAmountsListA[modifiedNode.index_in_temp_amount_list + resourceType] += inPlaceSymbols.parameters[amountParameters, resourceType];
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
                    if (branchSymbolParentStack.Count <= 0)
                    {
                        // uh oh. idk how this is happening but it is. probably related to the volumetric destruction and autophagy.
                        break;
                    }
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
#endif

    }

}
