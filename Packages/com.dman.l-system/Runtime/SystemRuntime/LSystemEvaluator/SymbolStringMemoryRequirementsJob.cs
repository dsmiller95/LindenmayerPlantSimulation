using System;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dman.LSystem.Extern;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    /// <summary>
    /// Step 2. match all rules which could possibly match, without checking the conditional expressions
    /// </summary>
    [BurstCompile]
    public struct SymbolStringMemoryRequirementsJob : IJob
    {
        public NativeArray<int> parameterTotalSum;
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        [ReadOnly]
        public NativeHashMap<int, MaxMatchMemoryRequirements> memoryRequirementsPerSymbol;
        [ReadOnly]
        public SymbolString<float> sourceSymbolString;


        public void Execute()
        {
            var totalParametersAlocated = 0;
            for (int i = 0; i < sourceSymbolString.Length; i++)
            {
                var symbol = sourceSymbolString[i];
                var matchData = new LSystemSingleSymbolMatchData()
                {
                    tmp_parameter_memory_space = JaggedIndexing.GetWithNoLength(totalParametersAlocated)
                };
                if (memoryRequirementsPerSymbol.TryGetValue(symbol, out var memoryRequirements))
                {
                    totalParametersAlocated += memoryRequirements.maxParameters;
                    matchData.is_trivial = false;
                }
                else
                {
                    matchData.is_trivial = true;
                    matchData.tmp_parameter_memory_space.length = 0;
                }
                matchSingletonData[i] = matchData;
            }

            parameterTotalSum[0] = totalParametersAlocated;
        }
    }
}
