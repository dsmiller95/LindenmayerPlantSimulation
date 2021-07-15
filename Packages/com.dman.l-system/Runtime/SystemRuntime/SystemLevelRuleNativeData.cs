using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime
{
    public struct SystemLevelRuleNativeData : INativeDisposable
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> suffixMatcherChildrenDataArray;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<SymbolMatcherGraphNode> suffixMatcherGraphNodeData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<InputSymbol.Blittable> prefixMatcherSymbols;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<RuleOutcome.Blittable> ruleOutcomeMemorySpace;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<ReplacementSymbolGenerator.Blittable> replacementsSymbolMemorySpace;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<StructExpression> structExpressionMemorySpace;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<OperatorDefinition> dynamicOperatorMemory;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeOrderedMultiDictionary<BasicRule.Blittable> blittableRulesByTargetSymbol;

        public NativeHashMap<int, MaxMatchMemoryRequirements> maxParameterMemoryRequirementsPerSymbol;

        public SystemLevelRuleNativeData(IEnumerable<BasicRule> rulesToWrite)
        {
            var allData = rulesToWrite.ToArray();
            var memReqs = allData.Aggregate(new RuleDataRequirements(), (agg, curr) => agg + curr.RequiredMemorySpace);
            prefixMatcherSymbols = new NativeArray<InputSymbol.Blittable>(memReqs.prefixNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherGraphNodeData = new NativeArray<SymbolMatcherGraphNode>(memReqs.suffixGraphNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherChildrenDataArray = new NativeArray<int>(memReqs.suffixChildren, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ruleOutcomeMemorySpace = new NativeArray<RuleOutcome.Blittable>(memReqs.ruleOutcomes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            replacementsSymbolMemorySpace = new NativeArray<ReplacementSymbolGenerator.Blittable>(memReqs.replacementSymbolsMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            structExpressionMemorySpace = new NativeArray<StructExpression>(memReqs.structExpressionMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            dynamicOperatorMemory = new NativeArray<OperatorDefinition>(memReqs.operatorMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            blittableRulesByTargetSymbol = default;
            maxParameterMemoryRequirementsPerSymbol = default;
        }
        public SystemLevelRuleNativeData(RuleDataRequirements memReqs)
        {
            prefixMatcherSymbols = new NativeArray<InputSymbol.Blittable>(memReqs.prefixNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherGraphNodeData = new NativeArray<SymbolMatcherGraphNode>(memReqs.suffixGraphNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherChildrenDataArray = new NativeArray<int>(memReqs.suffixChildren, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ruleOutcomeMemorySpace = new NativeArray<RuleOutcome.Blittable>(memReqs.ruleOutcomes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            replacementsSymbolMemorySpace = new NativeArray<ReplacementSymbolGenerator.Blittable>(memReqs.replacementSymbolsMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            structExpressionMemorySpace = new NativeArray<StructExpression>(memReqs.structExpressionMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            dynamicOperatorMemory = new NativeArray<OperatorDefinition>(memReqs.operatorMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            blittableRulesByTargetSymbol = default;
            maxParameterMemoryRequirementsPerSymbol = default;
        }

        public void Dispose()
        {
            suffixMatcherChildrenDataArray.Dispose();
            suffixMatcherGraphNodeData.Dispose();
            prefixMatcherSymbols.Dispose();
            ruleOutcomeMemorySpace.Dispose();
            dynamicOperatorMemory.Dispose();
            structExpressionMemorySpace.Dispose();
            replacementsSymbolMemorySpace.Dispose();
            if (blittableRulesByTargetSymbol.IsCreated) blittableRulesByTargetSymbol.Dispose();
            if (maxParameterMemoryRequirementsPerSymbol.IsCreated) maxParameterMemoryRequirementsPerSymbol.Dispose();
        }
        public JobHandle Dispose(JobHandle dependency)
        {
            // TODO
            suffixMatcherChildrenDataArray.Dispose();
            suffixMatcherGraphNodeData.Dispose();
            prefixMatcherSymbols.Dispose();
            ruleOutcomeMemorySpace.Dispose();
            dynamicOperatorMemory.Dispose();
            structExpressionMemorySpace.Dispose();
            replacementsSymbolMemorySpace.Dispose();
            if (blittableRulesByTargetSymbol.IsCreated) blittableRulesByTargetSymbol.Dispose();
            if (maxParameterMemoryRequirementsPerSymbol.IsCreated) maxParameterMemoryRequirementsPerSymbol.Dispose();
            return dependency;
        }
    }

    public struct RuleDataRequirements
    {
        public int suffixChildren;
        public int suffixGraphNodes;
        public int prefixNodes;
        public int ruleOutcomes;

        public int replacementSymbolsMemory;
        public int structExpressionMemory;
        public int operatorMemory;

        public static RuleDataRequirements operator +(RuleDataRequirements a, RuleDataRequirements b)
        {
            return new RuleDataRequirements
            {
                suffixGraphNodes = a.suffixGraphNodes + b.suffixGraphNodes,
                suffixChildren = a.suffixChildren + b.suffixChildren,
                prefixNodes = a.prefixNodes + b.prefixNodes,
                ruleOutcomes = a.ruleOutcomes + b.ruleOutcomes,

                replacementSymbolsMemory = a.replacementSymbolsMemory + b.replacementSymbolsMemory,
                structExpressionMemory = a.structExpressionMemory + b.structExpressionMemory,
                operatorMemory = a.operatorMemory + b.operatorMemory,
            };
        }
    }

    public class SymbolSeriesMatcherNativeDataWriter
    {
        public int indexInSuffixChildren = 0;
        public int indexInSuffixNodes = 0;
        public int indexInPrefixNodes = 0;
        public int indexInRuleOutcomes = 0;
        public int indexInReplacementSymbolsMemory = 0;
        public int indexInStructExpressionMemory = 0;
        public int indexInOperatorMemory = 0;
    }
}
