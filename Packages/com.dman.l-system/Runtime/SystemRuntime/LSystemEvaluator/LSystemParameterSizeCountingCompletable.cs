using System;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
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
    /// Class used to track intermediate state during the lsystem step. accessed from multiple job threads
    /// beware of multithreading
    /// </summary>
    public class LSystemParameterSizeCountingCompletable
    {
        public static async UniTask<LSystemState<float>> Run(
            LSystemState<float> systemState,
            DependencyTracker<SystemLevelRuleNativeData> lSystemNativeData,
            float[] globalParameters,
            ISet<int>[] includedCharactersByRuleIndex,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency,
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            
            JobHandle currentJobHandle = default;

            var paramModificationDependency = parameterModificationJobDependency;

            var nativeData = lSystemNativeData;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("Parameter counts");

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(systemState.currentSymbols.Data.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var parameterTotalSum = new NativeArray<int>(1, Allocator.TempJob);
            UnityEngine.Profiling.Profiler.EndSample();

            var memorySizeJob = new SymbolStringMemoryRequirementsJob
            {
                matchSingletonData = matchSingletonData,
                memoryRequirementsPerSymbol = nativeData.Data.maxParameterMemoryRequirementsPerSymbol,
                parameterTotalSum = parameterTotalSum,
                sourceSymbolString = systemState.currentSymbols.Data
            };

            currentJobHandle = memorySizeJob.Schedule();
            systemState.currentSymbols.RegisterDependencyOnData(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);


            UnityEngine.Profiling.Profiler.EndSample();

            // 2.1
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            var branchingCache = new SymbolStringBranchingCache(
                customSymbols.branchOpenSymbol,
                customSymbols.branchCloseSymbol,
                includedCharactersByRuleIndex,
                nativeData.Data);
            branchingCache.BuildJumpIndexesFromSymbols(systemState.currentSymbols);
            UnityEngine.Profiling.Profiler.EndSample();

            using var cancelJobSource = CancellationTokenSource.CreateLinkedTokenSource(forceSynchronous, cancel);
            try
            {
                var cancelled = await currentJobHandle.AwaitCompleteImmediateOnCancel(cancelJobSource.Token, 3);
                if (cancelled && cancel.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            catch(Exception)
            {
                currentJobHandle.Complete();
                matchSingletonData.Dispose();
                parameterTotalSum.Dispose();
                if (branchingCache.IsCreated) branchingCache.Dispose();
            }
            
            
            var paramTotal = parameterTotalSum[0];
            parameterTotalSum.Dispose();

            var nextCompletable = new LSystemRuleMatchCompletable(
                matchSingletonData,
                paramTotal,
                branchingCache,
                systemState,
                nativeData,
                globalParameters,
                customSymbols,
                paramModificationDependency);
            
            return await nextCompletable.ToUniTask(forceSynchronous, cancel);
        }
    }


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
