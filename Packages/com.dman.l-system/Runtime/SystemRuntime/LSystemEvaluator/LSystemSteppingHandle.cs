using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    /// <summary>
    /// Class used to track intermediate state during the lsystem step. accessed from multiple job threads
    /// beware of multithreading
    /// </summary>
    public class LSystemRuleMatchCompletable: ICompletable<LSystemState<float>>
    {
        public DependencyTracker<SymbolString<float>> sourceSymbolString;
        public Unity.Mathematics.Random randResult;

        /// <summary>
        /// A job handle too the <see cref="RuleReplacementSizeJob"/> which will count up the total replacement size
        /// </summary>
        public JobHandle preAllocationStep;


        /////////////// things owned by this step /////////
        public NativeArray<int> totalSymbolCount;
        public NativeArray<int> totalSymbolParameterCount;
        public SymbolStringBranchingCache branchingCache;


        /////////////// things transferred to the next step /////////
        public NativeArray<float> globalParamNative;
        public NativeArray<float> tmpParameterMemory;
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        /////////////// l-system native data /////////
        public DependencyTracker<SystemLevelRuleNativeData> nativeData;


        public JobHandle currentJobHandle => preAllocationStep;


        public LSystemRuleMatchCompletable(
            LSystemState<float> systemState,
            DependencyTracker<SystemLevelRuleNativeData> lSystemNativeData,
            float[] globalParameters,
            IDictionary<int, MaxMatchMemoryRequirements> maxMemoryRequirementsPerSymbol,
            int branchOpenSymbol,
            int branchCloseSymbol,
            ISet<int> ignoredCharacters)
        {
            this.randResult = systemState.randomProvider;
            this.sourceSymbolString = systemState.currentSymbols;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("Paramter counts");
            var matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(systemState.currentSymbols.Data.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var parameterTotalSum = 0;
            var possibleMatchesTotalSum = 0;
            for (int i = 0; i < systemState.currentSymbols.Data.Length; i++)
            {
                var symbol = systemState.currentSymbols.Data[i];
                var matchData = new LSystemSingleSymbolMatchData()
                {
                    tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(parameterTotalSum)
                };
                if (maxMemoryRequirementsPerSymbol.TryGetValue(symbol, out var memoryRequirements))
                {
                    parameterTotalSum += memoryRequirements.maxParameters;
                    possibleMatchesTotalSum += memoryRequirements.maxPossibleMatches;
                    matchData.isTrivial = false;
                }
                else
                {
                    matchData.isTrivial = true;
                    matchData.tmpParameterMemorySpace.length = 0;
                }
                matchSingletonData[i] = matchData;
            }

            this.tmpParameterMemory = new NativeArray<float>(parameterTotalSum, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            UnityEngine.Profiling.Profiler.EndSample();

            // 2.
            UnityEngine.Profiling.Profiler.BeginSample("matching");
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            this.branchingCache = new SymbolStringBranchingCache(
                branchOpenSymbol,
                branchCloseSymbol,
                ignoredCharacters,
                lSystemNativeData.Data);
            branchingCache.BuildJumpIndexesFromSymbols(systemState.currentSymbols);
            UnityEngine.Profiling.Profiler.EndSample();


            
            var globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);
            this.nativeData = lSystemNativeData;

            var prematchJob = new RuleCompleteMatchJob
            {
                matchSingletonData = matchSingletonData,

                sourceData = systemState.currentSymbols.Data,
                tmpParameterMemory = this.tmpParameterMemory,

                globalOperatorData = lSystemNativeData.Data.dynamicOperatorMemory,
                outcomes = lSystemNativeData.Data.ruleOutcomeMemorySpace,
                globalParams = globalParamNative,

                blittableRulesByTargetSymbol = lSystemNativeData.Data.blittableRulesByTargetSymbol,
                branchingCache = branchingCache,
                seed = this.randResult.NextUInt()
            };

            var matchingJobHandle = prematchJob.ScheduleBatch(
                matchSingletonData.Length,
                100);


            UnityEngine.Profiling.Profiler.EndSample();

            // 4.
            UnityEngine.Profiling.Profiler.BeginSample("replacement counting");

            var totalSymbolLength = new NativeArray<int>(1, Allocator.Persistent);
            var totalSymbolParameterCount = new NativeArray<int>(1, Allocator.Persistent);
            this.totalSymbolCount = totalSymbolLength;
            this.totalSymbolParameterCount = totalSymbolParameterCount;

            var totalSymbolLengthJob = new RuleReplacementSizeJob
            {
                matchSingletonData = matchSingletonData,
                totalResultSymbolCount = totalSymbolLength,
                totalResultParameterCount = totalSymbolParameterCount,
                sourceParameterIndexes = systemState.currentSymbols.Data.newParameters.indexing,
            };
            var totalSymbolLengthDependency = totalSymbolLengthJob.Schedule(matchingJobHandle);
            systemState.currentSymbols.RegisterDependencyOnData(totalSymbolLengthDependency);
            lSystemNativeData.RegisterDependencyOnData(totalSymbolLengthDependency);

            this.preAllocationStep = totalSymbolLengthDependency;

            UnityEngine.Profiling.Profiler.EndSample();

            this.matchSingletonData = matchSingletonData;
            this.globalParamNative = globalParamNative;

            UnityEngine.Profiling.Profiler.EndSample();

        }

        public ICompletable<LSystemState<float>> StepNext()
        {
            preAllocationStep.Complete();

            var totalNewSymbolSize = totalSymbolCount[0];
            var totalNewParamSize = totalSymbolParameterCount[0];
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();
            branchingCache.Dispose();

            return new LSystemSymbolReplacementCompletable(
                randResult,
                sourceSymbolString,
                totalNewSymbolSize,
                totalNewParamSize,
                globalParamNative,
                tmpParameterMemory,
                matchSingletonData,
                nativeData)
            {
                randResult = randResult,
            };
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
            preAllocationStep.Complete();
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();
            globalParamNative.Dispose();
            tmpParameterMemory.Dispose();
            matchSingletonData.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
            return inputDeps;
        }

        public void Dispose()
        {
            preAllocationStep.Complete();
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();
            globalParamNative.Dispose();
            tmpParameterMemory.Dispose();
            matchSingletonData.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
        }
    }


    public class LSystemSymbolReplacementCompletable : ICompletable<LSystemState<float>>
    {
        public DependencyTracker<SymbolString<float>> sourceSymbolString;
        public Unity.Mathematics.Random randResult;

        public JobHandle finalDependency;
        

        /////////////// things owned by this step /////////
        private SymbolString<float> target;

        /////////////// l-system native data /////////
        public DependencyTracker<SystemLevelRuleNativeData> nativeData;

        public JobHandle currentJobHandle => finalDependency;

        public LSystemSymbolReplacementCompletable(
            Unity.Mathematics.Random randResult,
            DependencyTracker<SymbolString<float>> sourceSymbolString,
            int totalNewSymbolSize,
            int totalNewParamSize,
            NativeArray<float> globalParamNative,
            NativeArray<float> tmpParameterMemory,
            NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
            DependencyTracker<SystemLevelRuleNativeData> nativeData)
        {
            this.target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);
            
            this.randResult = randResult;
            this.sourceSymbolString = sourceSymbolString;

            this.nativeData = nativeData;

            // 5
            UnityEngine.Profiling.Profiler.BeginSample("generating replacements");

            var replacementJob = new RuleReplacementJob
            {
                globalParametersArray = globalParamNative,

                parameterMatchMemory = tmpParameterMemory,
                matchSingletonData = matchSingletonData,

                sourceData = sourceSymbolString.Data,
                structExpressionSpace = nativeData.Data.structExpressionMemorySpace,
                globalOperatorData = nativeData.Data.dynamicOperatorMemory,
                replacementSymbolData = nativeData.Data.replacementsSymbolMemorySpace,
                outcomeData = nativeData.Data.ruleOutcomeMemorySpace,

                targetData = target,
                blittableRulesByTargetSymbol = nativeData.Data.blittableRulesByTargetSymbol
            };

            finalDependency = replacementJob.Schedule(
                matchSingletonData.Length,
                100
            );
            sourceSymbolString.RegisterDependencyOnData(finalDependency);
            nativeData.RegisterDependencyOnData(finalDependency);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable<LSystemState<float>> StepNext()
        {
            finalDependency.Complete();
            var newResult = new LSystemState<float>
            {
                randomProvider = randResult,
                currentSymbols = new DependencyTracker<SymbolString<float>>(target)
            };
            return new CompleteCompletable<LSystemState<float>>(newResult);
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
            //TODO
            finalDependency.Complete();
            target.Dispose();
            return inputDeps;
        }

        public void Dispose()
        {
            finalDependency.Complete();
            target.Dispose();
        }
    }
}
