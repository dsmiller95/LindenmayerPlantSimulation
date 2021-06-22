using Dman.LSystem.SystemRuntime.NativeCollections;
using System;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    public struct DiffusionRule
    {
        /// <summary>
        /// apply the diffusion rule.
        /// 
        /// assuming N is the diffusion node, and 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="branchingCache"></param>
        /// <param name="indexInSource"></param>
        /// <param name="indexInTarget"></param>
        /// <param name="indexInTargetParameters"></param>
        /// <param name="resourceNodeSymbol"></param>
        /// <param name="resourceAmountSymbol"></param>
        public static void ApplyDiffusionAtIndex(
                SymbolString<float> source,
                SymbolString<float> target,
                SymbolStringBranchingCache branchingCache,
                int indexInSource,
                int indexInTarget,
                int indexInTargetParameters,
                CustomRuleSymbols customSymbols,
                TmpNativeStack<int> forwardSearchHelperStack)
        {
            // copy the existing state over
            target.symbols[indexInTarget] = customSymbols.diffusionNode;
            var originalParameters = source.parameters[indexInSource];
            var newParameters = target.parameters[indexInTarget] = new JaggedIndexing
            {
                index = indexInTargetParameters,
                length = originalParameters.length
            };
            // copy out of the source param array directly into the target
            for (int i = 0; i < originalParameters.length; i++)
            {
                target.parameters[newParameters, i] = source.parameters[originalParameters, i];
            }

            // iterate backwards towards the root, looking for another resource node
            for (int backwardsIndex = indexInSource - 1; backwardsIndex >= 0; backwardsIndex--)
            {
                var nextSymbol = source[backwardsIndex];
                if (nextSymbol == branchingCache.branchCloseSymbol)
                {
                    backwardsIndex = branchingCache.FindOpeningBranchIndexReadonly(backwardsIndex);
                    continue;
                }
                if (nextSymbol == customSymbols.diffusionNode)
                {
                    DoDiffusion(
                        source,
                        target,
                        originalParameters,
                        newParameters,
                        backwardsIndex);
                    break;
                }
            }

            forwardSearchHelperStack.Reset();
            // iterate forwards to find all leaves, as well as any amounts
            for (int forwardsIndex = indexInSource + 1; forwardsIndex < source.Length; forwardsIndex++)
            {
                var nextSymbol = source[forwardsIndex];
                if (nextSymbol == branchingCache.branchCloseSymbol)
                {
                    if (forwardSearchHelperStack.Count <= 0)
                    {
                        // reached the end of the structure which this node is part of
                        break;
                    }
                    // reached a closing symbol of a branch symbol which was previously popped onto the stack
                    forwardSearchHelperStack.Pop();
                }
                else if (nextSymbol == branchingCache.branchOpenSymbol)
                {
                    forwardSearchHelperStack.Push(forwardsIndex);
                }
                else if (nextSymbol == customSymbols.diffusionNode)
                {
                    DoDiffusion(
                        source,
                        target,
                        originalParameters,
                        newParameters,
                        forwardsIndex);
                    //done with this branch. pop to the next, or complete if we are on root branch
                    if(forwardSearchHelperStack.Count <= 0)
                    {
                        break;
                    }else
                    {
                        var openBranchIndex = forwardSearchHelperStack.Pop();
                        forwardsIndex = branchingCache.FindClosingBranchIndexReadonly(openBranchIndex);
                    }
                }
                else if (nextSymbol == customSymbols.diffusionAmount)
                {
                    DoResourceAddition(
                        source,
                        target,
                        originalParameters,
                        newParameters,
                        forwardsIndex);
                }
            }
        }
        private static void DoDiffusion(
                SymbolString<float> source,
                SymbolString<float> target,
                JaggedIndexing nodeASourceParameters,
                JaggedIndexing nodeATargetParameters,
                int nodeBIndexInSource
                )
        {
            var nodeBParameters = source.parameters[nodeBIndexInSource];
            var diffusionConstant = (source.parameters[nodeASourceParameters, 0] + source.parameters[nodeBParameters, 0]) / 2f;
            for (
                int resource = 0;
                resource * 2 + 1 < nodeASourceParameters.length && resource * 2 + 1 < nodeBParameters.length;
                resource++)
            {
                var resourceValueIndex = resource * 2 + 1;
                var resourceLimitIndex = resource * 2 + 1 + 1;

                var oldNodeAValue = source.parameters[nodeASourceParameters, resourceValueIndex];
                var nodeAValueCap = source.parameters[nodeASourceParameters, resourceLimitIndex];

                var oldNodeBValue = source.parameters[nodeBParameters, resourceValueIndex];
                var nodeBValueCap = source.parameters[nodeBParameters, resourceLimitIndex];

                var transferredAmount = diffusionConstant * (oldNodeBValue - oldNodeAValue);

                if (transferredAmount == 0)
                {
                    continue;
                }
                if (transferredAmount < 0 && oldNodeBValue >= nodeBValueCap)
                {
                    // the direction of flow is towards node B, and also node B is above its value cap. skip updating the resource on this connection completely.
                    continue;
                }
                if (transferredAmount > 0 && oldNodeAValue >= nodeAValueCap)
                {
                    // the direction of flow is towards node A, and also node A is above its value cap. skip updating the resource on this connection completely.
                    continue;
                }

                target.parameters[nodeATargetParameters, resourceValueIndex] += transferredAmount;
            }
        }
        private static void DoResourceAddition(
                SymbolString<float> source,
                SymbolString<float> target,
                JaggedIndexing nodeASourceParameters,
                JaggedIndexing nodeATargetParameters,
                int nodeBIndexInSource
                )
        {
            var amountParameters = source.parameters[nodeBIndexInSource];
            for (
                int resource = 0;
                resource * 2 + 1 < nodeASourceParameters.length && resource < amountParameters.length;
                resource++)
            {
                var resourceValueIndex = resource * 2 + 1;

                var amountToAdd = source.parameters[amountParameters, resource];
                target.parameters[nodeATargetParameters, resourceValueIndex] += amountToAdd;
            }
        }
    }
}
