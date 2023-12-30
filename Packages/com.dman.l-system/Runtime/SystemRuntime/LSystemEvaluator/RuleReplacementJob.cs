using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    [BurstCompile]
    public struct RuleReplacementJob : IJobParallelFor
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> globalParametersArray;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> parameterMatchMemory;

        [ReadOnly]
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<RuleOutcome.Blittable> outcomeData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<ReplacementSymbolGenerator.Blittable> replacementSymbolData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<StructExpression> structExpressionSpace;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<OperatorDefinition> globalOperatorData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public SymbolString<float> sourceData;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction] // disable all safety to allow parallel writes
        public SymbolString<float> targetData;

        [ReadOnly]
        public NativeOrderedMultiDictionary<BasicRule.Blittable> blittableRulesByTargetSymbol;
        [ReadOnly]
        public SymbolStringBranchingCache branchingCache;

        public CustomRuleSymbols customSymbols;

        public void Execute(int indexInSymbols)
        {
            var matchSingleton = matchSingletonData[indexInSymbols];
            var symbol = sourceData.symbols[indexInSymbols];
            if (matchSingleton.is_trivial)
            {
                var targetIndex = matchSingleton.replacement_symbol_indexing.index;
                // check for custom rules
                if (customSymbols.hasDiffusion && !customSymbols.independentDiffusionUpdate)
                {
                    // let the diffusion library handle these updates. only if diffusion is happening in parallel
                    if (symbol == customSymbols.diffusionNode || symbol == customSymbols.diffusionAmount)
                    {
                        return;
                    }
                }

                // match is trivial. just copy the existing symbol and parameters over, nothing else.
                targetData.symbols[targetIndex] = symbol;
                var sourceParamIndexer = sourceData.parameters[indexInSymbols];
                var targetDataIndexer = new JaggedIndexing
                {
                    index = matchSingleton.replacement_parameter_indexing.index,
                    length = sourceParamIndexer.length
                };
                targetData.parameters[targetIndex] = targetDataIndexer;
                // when trivial, copy out of the source param array directly. As opposed to reading parameters out of the parameterMatchMemory when evaluating
                //      a non-trivial match
                for (int i = 0; i < sourceParamIndexer.length; i++)
                {
                    targetData.parameters[targetDataIndexer, i] = sourceData.parameters[sourceParamIndexer, i];
                }
                return;
            }

            if (!blittableRulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList.length <= 0)
            {
                //throw new System.Exception(LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME.ToString());

                return;
            }

            var rule = blittableRulesByTargetSymbol[ruleList, matchSingleton.matched_rule_index_in_possible];

            rule.WriteReplacementSymbols(
                globalParametersArray,
                parameterMatchMemory,
                targetData,
                matchSingleton,
                globalOperatorData,
                replacementSymbolData,
                outcomeData,
                structExpressionSpace
                );
        }
    }
}
