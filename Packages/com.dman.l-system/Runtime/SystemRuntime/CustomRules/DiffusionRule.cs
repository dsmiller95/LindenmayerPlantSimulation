using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    public struct DiffusionRule
    {

        public static void ExtractEdgesAndNodes(
                SymbolString<float> source,
                NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
                SymbolStringBranchingCache branchingCache,
                CustomRuleSymbols customSymbols,
                DiffusionWorkingDataPack workingData)
        {
            var branchSymbolParentStack = new TmpNativeStack<BranchEvent>(5);
            var currentNodeParent = -1;

            for (int symbolIndex = 0; symbolIndex < source.Length; symbolIndex++)
            {
                var symbol = source[symbolIndex];
                if(symbol == customSymbols.diffusionNode)
                {
                    if (currentNodeParent >= 0)
                    {
                        var newEdge = new DiffusionEdge
                        {
                            nodeAIndex = currentNodeParent,
                            nodeBIndex = workingData.nodes.Length
                        };
                        workingData.allEdges.Add(newEdge);
                    }
                    currentNodeParent = workingData.nodes.Length;

                    var nodeParams = source.parameters[symbolIndex];
                    var nodeSingleton = matchSingletonData[symbolIndex];

                    var newNode = new DiffusionNode
                    {
                        indexInTarget = nodeSingleton.replacementSymbolIndexing.index,
                        targetParameters = nodeSingleton.replacementParameterIndexing,

                        indexInTempAmountList = workingData.nodeAmountsListA.Length,

                        totalResourceTypes = (nodeParams.length - 1) / 2,
                        diffusionConstant = source.parameters[nodeParams, 0],
                    };
                    newNode.targetParameters.length = nodeParams.length;
                    workingData.nodes.Add(newNode);

                    for (int resourceType = 0; resourceType < newNode.totalResourceTypes; resourceType++)
                    {
                        var currentAmount = source.parameters[nodeParams, resourceType * 2 + 1];
                        var maxCapacity = source.parameters[nodeParams, resourceType * 2 + 1 + 1];
                        workingData.nodeAmountsListA.Add(currentAmount);
                        workingData.nodeAmountsListB.Add(0);
                        workingData.nodeMaxCapacities.Add(maxCapacity);
                    }

                }else if (symbol == customSymbols.diffusionAmount)
                {
                    if(currentNodeParent < 0)
                    {
                        // problem: the amount will dissapear
                        continue;
                    }
                    var modifiedNode = workingData.nodes[currentNodeParent];
                    var amountParameters = source.parameters[symbolIndex];
                    for (int resourceType = 0; resourceType < modifiedNode.totalResourceTypes && resourceType < amountParameters.length; resourceType++)
                    {
                        workingData.nodeAmountsListA[modifiedNode.indexInTempAmountList + resourceType] += source.parameters[amountParameters, resourceType];
                    }
                }else if(symbol == branchingCache.branchOpenSymbol)
                {
                    branchSymbolParentStack.Push(new BranchEvent
                    {
                        openBranchSymbolIndex = symbolIndex,
                        currentNodeParent = currentNodeParent
                    });
                }else if(symbol == branchingCache.branchCloseSymbol)
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

        public static void DiffuseBetween(DiffusionWorkingDataPack workingData, bool diffuseAtoB)
        {
            var sourceDiffuseAmounts = diffuseAtoB ? workingData.nodeAmountsListA : workingData.nodeAmountsListB;
            var targetDiffuseAmounts = diffuseAtoB ? workingData.nodeAmountsListB : workingData.nodeAmountsListA;

            targetDiffuseAmounts.CopyFrom(sourceDiffuseAmounts);

            for (int edgeIndex = 0; edgeIndex < workingData.allEdges.Length; edgeIndex++)
            {
                var edge = workingData.allEdges[edgeIndex];
                DiffuseAcrossEdge(workingData, edge, sourceDiffuseAmounts, targetDiffuseAmounts);
            }
        }

        private static void DiffuseAcrossEdge(
            DiffusionWorkingDataPack workingData,
            DiffusionEdge edge,
            NativeList<float> sourceAmounts,
            NativeList<float> targetAmounts
            )
        {
            var nodeA = workingData.nodes[edge.nodeAIndex];
            var nodeB = workingData.nodes[edge.nodeBIndex];

            var diffusionConstant = (nodeA.diffusionConstant + nodeB.diffusionConstant) / 2f;
            for (
                int resource = 0;
                resource < nodeA.totalResourceTypes && resource < nodeA.totalResourceTypes;
                resource++)
            {
                var oldNodeAValue = sourceAmounts[nodeA.indexInTempAmountList + resource];
                var nodeAValueCap = workingData.nodeMaxCapacities[nodeA.indexInTempAmountList + resource];

                var oldNodeBValue = sourceAmounts[nodeB.indexInTempAmountList + resource];
                var nodeBValueCap = workingData.nodeMaxCapacities[nodeB.indexInTempAmountList + resource];

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

        public static void ApplyDiffusionResults(
            SymbolString<float> target,
            DiffusionWorkingDataPack workingData,
            CustomRuleSymbols customSymbols,
            bool latestDataIsInA)
        {
            var amountData = latestDataIsInA ? workingData.nodeAmountsListA : workingData.nodeAmountsListB;
            for (int nodeIndex = 0; nodeIndex < workingData.nodes.Length; nodeIndex++)
            {
                var node = workingData.nodes[nodeIndex];
                target[node.indexInTarget] = customSymbols.diffusionNode;

                target.parameters[node.indexInTarget] = node.targetParameters;

                target.parameters[node.targetParameters, 0] = node.diffusionConstant;
                for (int resourceType = 0; resourceType < node.totalResourceTypes; resourceType++)
                {
                    target.parameters[node.targetParameters, resourceType * 2 + 1] = amountData[node.indexInTempAmountList + resourceType];
                    target.parameters[node.targetParameters, resourceType * 2 + 2] = workingData.nodeMaxCapacities[node.indexInTempAmountList + resourceType];
                }
            }
        }
    }
}
