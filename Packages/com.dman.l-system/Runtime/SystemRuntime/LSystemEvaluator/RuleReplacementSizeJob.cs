using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    
    [BurstCompile]
    public struct RuleReplacementSizeJob : IJob
    {
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;
        public NativeArray<int> totalResultSymbolCount;
        public NativeArray<int> totalResultParameterCount;

        public SymbolString<float> sourceData;

        public CustomRuleSymbols customSymbols;
        public void Execute()
        {
            var sourceParameterIndexes = sourceData.parameters;
            var totalResultSymbolSize = 0;
            var totalResultParamSize = 0;
            for (int i = 0; i < matchSingletonData.Length; i++)
            {
                var singleton = matchSingletonData[i];
                singleton.replacement_symbol_indexing.index = totalResultSymbolSize;
                singleton.replacement_parameter_indexing.index = totalResultParamSize;
                matchSingletonData[i] = singleton;
                if (!singleton.is_trivial)
                {
                    totalResultSymbolSize += singleton.replacement_symbol_indexing.length;
                    totalResultParamSize += singleton.replacement_parameter_indexing.length;
                    continue;
                }
                // custom rules
                if (customSymbols.hasDiffusion && !customSymbols.independentDiffusionUpdate && customSymbols.diffusionAmount == sourceData[i])
                {
                    // if matching the diffusion's amount symbol, and the update is happening in parallel, remove all the parameters.
                    //  leaving just the symbol.
                    //  this is to ensure closest possible consistency with the independent diffusion update code
                    // will copy 0 parameters over, the symbol remains.
                    // only do this if the diffusion update is happening in parallel to the regular system step
                    totalResultSymbolSize += 1;
                    continue;
                }
                // default behavior
                totalResultSymbolSize += 1;
                totalResultParamSize += sourceParameterIndexes[i].length;
            }

            totalResultSymbolCount[0] = totalResultSymbolSize;
            totalResultParameterCount[0] = totalResultParamSize;
        }
    }

}
