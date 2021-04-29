using Dman.LSystem.SystemRuntime.DynamicExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct SystemLevelRuleNativeData: IDisposable
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
        public NativeArray<OperatorDefinition> dynamicOperatorMemory;

        public SystemLevelRuleNativeData(IEnumerable<BasicRule> rulesToWrite)
        {
            var allData = rulesToWrite.ToArray();
            var memReqs = allData.Aggregate(new RuleDataRequirements(), (agg, curr) => agg + curr.RequiredMemorySpace);
            prefixMatcherSymbols = new NativeArray<InputSymbol.Blittable>(memReqs.prefixNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherGraphNodeData = new NativeArray<SymbolMatcherGraphNode>(memReqs.suffixGraphNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherChildrenDataArray = new NativeArray<int>(memReqs.suffixChildren, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ruleOutcomeMemorySpace = new NativeArray<RuleOutcome.Blittable>(memReqs.ruleOutcomes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            dynamicOperatorMemory = new NativeArray<OperatorDefinition>(memReqs.operatorMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            isDisposed = false;
        }
        public SystemLevelRuleNativeData(RuleDataRequirements memReqs)
        {
            prefixMatcherSymbols = new NativeArray<InputSymbol.Blittable>(memReqs.prefixNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherGraphNodeData = new NativeArray<SymbolMatcherGraphNode>(memReqs.suffixGraphNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherChildrenDataArray = new NativeArray<int>(memReqs.suffixChildren, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ruleOutcomeMemorySpace = new NativeArray<RuleOutcome.Blittable>(memReqs.ruleOutcomes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            dynamicOperatorMemory = new NativeArray<OperatorDefinition>(memReqs.operatorMemory, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            isDisposed = false;
        }

        private bool isDisposed;
        public void Dispose()
        {
            if (isDisposed) return;
            suffixMatcherChildrenDataArray.Dispose();
            suffixMatcherGraphNodeData.Dispose();
            prefixMatcherSymbols.Dispose();
            ruleOutcomeMemorySpace.Dispose();
            isDisposed = true;
        }
    }

    public struct RuleDataRequirements
    {
        public int suffixChildren;
        public int suffixGraphNodes;
        public int prefixNodes;
        public int ruleOutcomes;
        public int operatorMemory;

        public static RuleDataRequirements operator +(RuleDataRequirements a, RuleDataRequirements b)
        {
            return new RuleDataRequirements
            {
                suffixGraphNodes = a.suffixGraphNodes + b.suffixGraphNodes,
                suffixChildren = a.suffixChildren + b.suffixChildren,
                prefixNodes = a.prefixNodes + b.prefixNodes,
                ruleOutcomes = a.ruleOutcomes + b.ruleOutcomes,
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
        public int indexInOperatorMemory = 0;
    }
}
