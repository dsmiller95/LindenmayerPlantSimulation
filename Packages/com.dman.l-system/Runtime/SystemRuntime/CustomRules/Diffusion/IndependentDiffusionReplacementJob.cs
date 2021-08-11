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

        public int branchOpenSymbol;
        public int branchCloseSymbol;

        internal DiffusionWorkingDataPack working;

        public CustomRuleSymbols customSymbols;


        public void Execute()
        {
            if (customSymbols.hasDiffusion && customSymbols.independentDiffusionUpdate)
            {
                ExtractEdgesAndNodes();
                working.PerformDiffusionOnDataAndApply(inPlaceSymbols);
            }
        }

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
                        var newEdge = new DiffusionEdge
                        {
                            nodeAIndex = currentNodeParent,
                            nodeBIndex = working.nodes.Length
                        };
                        working.allEdges.Add(newEdge);
                    }
                    currentNodeParent = working.nodes.Length;

                    var nodeParams = inPlaceSymbols.parameters[symbolIndex];

                    var newNode = new DiffusionNode
                    {
                        indexInTarget = symbolIndex,
                        targetParameters = nodeParams,

                        indexInTempAmountList = working.nodeAmountsListA.Length,

                        totalResourceTypes = (nodeParams.length - 1) / 2,
                        diffusionConstant = inPlaceSymbols.parameters[nodeParams, 0],
                    };
                    newNode.targetParameters.length = nodeParams.length;
                    working.nodes.Add(newNode);

                    for (int resourceType = 0; resourceType < newNode.totalResourceTypes; resourceType++)
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
                    var modifiedNode = working.nodes[currentNodeParent];
                    var amountParameters = inPlaceSymbols.parameters[symbolIndex];
                    inPlaceSymbols.parameters[symbolIndex] = new JaggedIndexing
                    {
                        index = amountParameters.index,
                        length = 0
                    };
                    if (currentNodeParent < 0)
                    {
                        // problem: the amount will dissapear
                        continue;
                    }
                    for (int resourceType = 0; resourceType < modifiedNode.totalResourceTypes && resourceType < amountParameters.length; resourceType++)
                    {
                        working.nodeAmountsListA[modifiedNode.indexInTempAmountList + resourceType] += inPlaceSymbols.parameters[amountParameters, resourceType];
                    }
                }
                else if (symbol == branchOpenSymbol)
                {
                    branchSymbolParentStack.Push(new BranchEvent
                    {
                        openBranchSymbolIndex = symbolIndex,
                        currentNodeParent = currentNodeParent
                    });
                }
                else if (symbol == branchCloseSymbol)
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
