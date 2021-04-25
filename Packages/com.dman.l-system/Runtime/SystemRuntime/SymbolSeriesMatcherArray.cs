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

        public SystemLevelRuleNativeData(IEnumerable<BasicRule> rulesToWrite)
        {
            var allData = rulesToWrite.ToArray();
            var memReqs = allData.Aggregate(new RuleDataRequirements(), (agg, curr) => agg + curr.RequiredMemorySpace);
            prefixMatcherSymbols = new NativeArray<InputSymbol.Blittable>(memReqs.prefixNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherGraphNodeData = new NativeArray<SymbolMatcherGraphNode>(memReqs.suffixGraphNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherChildrenDataArray = new NativeArray<int>(memReqs.suffixChildren, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        public SystemLevelRuleNativeData(RuleDataRequirements memReqs)
        {
            prefixMatcherSymbols = new NativeArray<InputSymbol.Blittable>(memReqs.prefixNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherGraphNodeData = new NativeArray<SymbolMatcherGraphNode>(memReqs.suffixGraphNodes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            suffixMatcherChildrenDataArray = new NativeArray<int>(memReqs.suffixChildren, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public void Dispose()
        {
            suffixMatcherChildrenDataArray.Dispose();
            suffixMatcherGraphNodeData.Dispose();
            prefixMatcherSymbols.Dispose();
        }
    }

    public struct RuleDataRequirements
    {
        public int suffixChildren;
        public int suffixGraphNodes;
        public int prefixNodes;

        public static RuleDataRequirements operator +(RuleDataRequirements a, RuleDataRequirements b)
        {
            return new RuleDataRequirements
            {
                suffixGraphNodes = a.suffixGraphNodes + b.suffixGraphNodes,
                suffixChildren = a.suffixChildren + b.suffixChildren,
                prefixNodes = a.prefixNodes + b.prefixNodes,
            };
        }
    }

    public class SymbolSeriesMatcherNativeDataWriter
    {
        public int indexInSuffixChildren = 0;
        public int indexInSuffixNodes = 0;
        public int indexInPrefixNodes = 0;
    }
}
