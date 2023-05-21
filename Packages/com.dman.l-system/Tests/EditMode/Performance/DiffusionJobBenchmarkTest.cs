using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using Dman.LSystem;
using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.CustomRules.Diffusion;
using Unity.Collections;
using Unity.Jobs;
using Unity.PerformanceTesting;

public class DiffusionJobBenchmarkTest
{
    class SymbolElement
    {
        public int symbol;
        public List<float> parameters;
    }

    List<float> GetDiffusionNodeParameters(float diffuseConstant, float amount, float max, int resourceCount)
    {
        var parameters = new List<float>();
        parameters.Add(diffuseConstant);
        for (int i = 0; i < resourceCount; i++)
        {
            parameters.Add(amount);
            parameters.Add(max);
        }
        return parameters;
    }

    SymbolString<float> FromElements(List<SymbolElement> elements)
    {
        var symbols = elements.Select(x => x.symbol).ToArray();
        var parameters = elements.Select(x => x.parameters.ToArray()).ToArray();
        return new SymbolString<float>(symbols, parameters, Allocator.Persistent);
    }
    
    
    [Test, Performance]
    public void Diffusion_10Deep_6Resource_1Iter_Performance(){
        LargeDiffusionNetworkPerformance(10, 6, 1);
    }
    
    [Test, Performance]
    public void Diffusion_10Deep_6Resource_10Iter_Performance(){
        LargeDiffusionNetworkPerformance(10, 6, 10);
    }
    
    [Test, Performance]
    public void Diffusion_10Deep_1Resource_1Iter_Performance(){
        LargeDiffusionNetworkPerformance(10, 1, 1);
    }
    [Test, Performance]
    public void Diffusion_10Deep_1Resource_10Iter_Performance(){
        LargeDiffusionNetworkPerformance(10, 1, 10);
    }
    
    public void LargeDiffusionNetworkPerformance(int depth, int resourcePerNode, int diffusionCycles)
    {
        var openBranchSymbol = 0;
        var closeBranchSymbol = 1;
        var diffusionNodeSymbol = 2;
        var diffusionAmountSymbol = 3;

        var b = new SymbolElement()
        {
            symbol = openBranchSymbol,
            parameters = new List<float>()
        };
        
        var d = new SymbolElement()
        {
            symbol = closeBranchSymbol,
            parameters = new List<float>()
        };


        var initialState = new List<SymbolElement>
        {
            new SymbolElement()
            {
                symbol = diffusionNodeSymbol,
                parameters = GetDiffusionNodeParameters(0.5f, 20f, 1000, resourcePerNode)
            }
        };

        for (int i = 0; i < depth; i++)
        {
            var nextState = new List<SymbolElement>
            {
                new SymbolElement()
                {
                    symbol = diffusionNodeSymbol,
                    parameters = GetDiffusionNodeParameters(0.5f, i * 25f, 1000, resourcePerNode)
                }
            };
            for (int j = 0; j < 2; j++)
            {
                nextState.Add(b);
                nextState.AddRange(initialState);
                nextState.Add(d);
            }

            initialState = nextState;
        }
        
        using var sourceString = FromElements(initialState);
        using var targetString = new SymbolString<float>(sourceString.symbols.Length, sourceString.parameters.data.Length, Allocator.Persistent);
        
        var matchSingletonDataList = new List<LSystemSingleSymbolMatchData>(sourceString.Length);
        for (int i = 0; i < sourceString.Length; i++)
        {
            matchSingletonDataList.Add(new LSystemSingleSymbolMatchData
            {
                is_trivial = true,
                replacement_symbol_indexing = new JaggedIndexing
                {
                    index = i,
                    length = 0
                },
                replacement_parameter_indexing = new JaggedIndexing
                {
                    index = sourceString.parameters.indexing[i].index,
                    length = 0
                },
                tmp_parameter_memory_space = new JaggedIndexing
                {
                    index = 0,
                    length = 0
                },
                matched_rule_index_in_possible = 0,
                selected_replacement_pattern = 0,
                error_code = LSystemMatchErrorCode.None
            });
        }
        using var matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(matchSingletonDataList.ToArray(), Allocator.Persistent);
        
        var customSymbols = new Dman.LSystem.SystemRuntime.CustomRules.CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionAmount = diffusionAmountSymbol,
            diffusionNode = diffusionNodeSymbol,
            diffusionStepsPerStep = diffusionCycles,
            diffusionConstantRuntimeGlobalMultiplier = 1f,
            branchOpenSymbol = openBranchSymbol,
            branchCloseSymbol = closeBranchSymbol,
        };
        
        
        Measure.Method(() =>
            {
                var diffusionHelper = new DiffusionWorkingDataPack();
#if !RUST_SUBSYSTEM
                diffusionHelper = new DiffusionWorkingDataPack(
                    10,
                    5,
                    2,
                    customSymbols,
                    Allocator.TempJob);
#endif
                var diffusionJob = new ParallelDiffusionReplacementJob
                {
                    matchSingletonData = matchSingletonData,
                    sourceData = sourceString,
                    targetData = targetString,
                    customSymbols = customSymbols,
#if !RUST_SUBSYSTEM
                    working = diffusionHelper
#endif
                };
                diffusionJob.Schedule().Complete();
#if !RUST_SUBSYSTEM
                diffusionHelper.Dispose();
#endif
            })
            .WarmupCount(10)
            .MeasurementCount(25)
            .IterationsPerMeasurement(10)
            .GC()
            .Run();
        
    }
}
