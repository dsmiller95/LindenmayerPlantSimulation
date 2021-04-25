using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct SymbolSeriesMatcherNativeDataArray: IDisposable
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> childrenDataArray;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<SymbolMatcherGraphNode> graphNodeData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<SymbolSeriesSuffixMatcher> singletonStructData;

        public SymbolSeriesMatcherNativeDataArray(IEnumerable<BasicRule> rulesToWrite)
        {
            var allData = rulesToWrite.ToArray();
            singletonStructData = new NativeArray<SymbolSeriesSuffixMatcher>(allData.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            graphNodeData = new NativeArray<SymbolMatcherGraphNode>(allData.Sum(x => x.RequiredGraphNodeMemSpace), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            childrenDataArray = new NativeArray<int>(allData.Sum(x => x.RequiredChildrenMemSpace), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        public SymbolSeriesMatcherNativeDataArray(IEnumerable<SymbolSeriesSuffixBuilder> matchers)
        {
            var allData = matchers.ToArray();
            singletonStructData = new NativeArray<SymbolSeriesSuffixMatcher>(allData.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            graphNodeData = new NativeArray<SymbolMatcherGraphNode>(allData.Sum(x => x.RequiredGraphNodeMemSpace), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            childrenDataArray = new NativeArray<int>(allData.Sum(x => x.RequiredChildrenMemSpace), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        public SymbolSeriesMatcherNativeDataArray(IEnumerable<SymbolSeriesPrefixBuilder> matchers)
        {
            singletonStructData = new NativeArray<SymbolSeriesSuffixMatcher>(0, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            graphNodeData = new NativeArray<SymbolMatcherGraphNode>(0, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            childrenDataArray = new NativeArray<int>(0, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public void Dispose()
        {
            childrenDataArray.Dispose();
            graphNodeData.Dispose();
            singletonStructData.Dispose();
        }
    }
    public class SymbolSeriesMatcherNativeDataWriter
    {
        public int indexInChildren = 0;
        public int indexInGraphNode = 0;
        public int indexInSingletons = 0;
    }
}
