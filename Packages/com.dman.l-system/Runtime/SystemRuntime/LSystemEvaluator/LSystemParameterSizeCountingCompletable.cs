using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    /// <summary>
    /// Class used to track intermediate state during the lsystem step. accessed from multiple job threads
    /// beware of multithreading
    /// </summary>
    public class LSystemParameterSizeCountingCompletable : ICompletable<LSystemState<float>>
    {
        private LSystemState<float> systemState;

        public DependencyTracker<SymbolString<float>> sourceSymbolString;
        public CustomRuleSymbols customSymbols;

        /////////////// things owned by this step /////////
        public NativeArray<int> parameterTotalSum;

        /////////////// things transferred to the next step /////////
        public SymbolStringBranchingCache branchingCache;
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        private float[] globalParameters;
        private JobHandle paramModificationDependency;


        /////////////// l-system native data /////////
        public DependencyTracker<SystemLevelRuleNativeData> nativeData;


        public JobHandle currentJobHandle { get; private set; }


        public LSystemParameterSizeCountingCompletable(
            LSystemState<float> systemState,
            DependencyTracker<SystemLevelRuleNativeData> lSystemNativeData,
            float[] globalParameters,
            int branchOpenSymbol,
            int branchCloseSymbol,
            ISet<int>[] includedCharactersByRuleIndex,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency)
        {
            currentJobHandle = default;

            this.globalParameters = globalParameters;
            this.paramModificationDependency = parameterModificationJobDependency;

            this.systemState = systemState;
            this.customSymbols = customSymbols;
            sourceSymbolString = systemState.currentSymbols;
            nativeData = lSystemNativeData;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("Paramter counts");
            matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(sourceSymbolString.Data.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            parameterTotalSum = new NativeArray<int>(1, Allocator.TempJob);
            var memorySizeJob = new SymbolStringMemoryRequirementsJob
            {
                matchSingletonData = matchSingletonData,
                memoryRequirementsPerSymbol = nativeData.Data.maxParameterMemoryRequirementsPerSymbol,
                parameterTotalSum = parameterTotalSum,
                sourceSymbolString = sourceSymbolString.Data
            };

            currentJobHandle = memorySizeJob.Schedule();
            sourceSymbolString.RegisterDependencyOnData(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);


            UnityEngine.Profiling.Profiler.EndSample();

            // 2.1
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            branchingCache = new SymbolStringBranchingCache(
                branchOpenSymbol,
                branchCloseSymbol,
                includedCharactersByRuleIndex,
                nativeData.Data);
            branchingCache.BuildJumpIndexesFromSymbols(sourceSymbolString);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();
            var paramTotal = parameterTotalSum[0];
            parameterTotalSum.Dispose();

            return new LSystemRuleMatchCompletable(
                matchSingletonData,
                paramTotal,
                branchingCache,
                systemState,
                nativeData,
                globalParameters,
                customSymbols,
                paramModificationDependency);
        }

        public bool IsComplete()
        {
            return false;
        }
        public bool HasErrored()
        {
            return false;
        }

        public string GetError()
        {
            return null;
        }

        public LSystemState<float> GetData()
        {
            return null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            // TODO
            currentJobHandle.Complete();
            matchSingletonData.Dispose();
            parameterTotalSum.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
            return inputDeps;
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            matchSingletonData.Dispose();
            parameterTotalSum.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
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
                    tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(totalParametersAlocated)
                };
                if (memoryRequirementsPerSymbol.TryGetValue(symbol, out var memoryRequirements))
                {
                    totalParametersAlocated += memoryRequirements.maxParameters;
                    matchData.isTrivial = false;
                }
                else
                {
                    matchData.isTrivial = true;
                    matchData.tmpParameterMemorySpace.length = 0;
                }
                matchSingletonData[i] = matchData;
            }

            parameterTotalSum[0] = totalParametersAlocated;
        }
    }
}
