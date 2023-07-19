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
    /// <summary>
    /// Class used to track intermediate state during the lsystem step. accessed from multiple job threads
    /// beware of multithreading
    /// </summary>
    public class LSystemRuleMatchCompletable
    {
        public static async UniTask<LSystemState<float>> Run(
            NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
            int parameterTotalSum,
            SymbolStringBranchingCache branchingCache,
            LSystemState<float> lastSystemState,
            DependencyTracker<SystemLevelRuleNativeData> lSystemNativeData,
            float[] globalParameters,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency,
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            
            var randResult = lastSystemState.randomProvider;
            var nativeData = lSystemNativeData;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var tmpParameterMemory = new NativeArray<float>(parameterTotalSum, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();

            // 2.
            UnityEngine.Profiling.Profiler.BeginSample("matching");


            var prematchJob = new RuleCompleteMatchJob
            {
                matchSingletonData = matchSingletonData,

                sourceData = lastSystemState.currentSymbols.Data,
                tmpParameterMemory = tmpParameterMemory,

                globalOperatorData = nativeData.Data.dynamicOperatorMemory,
                outcomes = nativeData.Data.ruleOutcomeMemorySpace,
                globalParams = globalParamNative,

                blittableRulesByTargetSymbol = nativeData.Data.blittableRulesByTargetSymbol,
                branchingCache = branchingCache,
                seed = randResult.NextUInt()
            };

            var matchingJobHandle = prematchJob.ScheduleBatch(
                matchSingletonData.Length,
                100,
                parameterModificationJobDependency);


            UnityEngine.Profiling.Profiler.EndSample();

            // 4.
            UnityEngine.Profiling.Profiler.BeginSample("replacement counting");

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var totalSymbolCount = new NativeArray<int>(1, Allocator.Persistent);
            var totalSymbolParameterCount = new NativeArray<int>(1, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();

            var totalSymbolLengthJob = new RuleReplacementSizeJob
            {
                matchSingletonData = matchSingletonData,
                totalResultSymbolCount = totalSymbolCount,
                totalResultParameterCount = totalSymbolParameterCount,
                sourceData = lastSystemState.currentSymbols.Data,
                customSymbols = customSymbols
            };
            var currentJobHandle = totalSymbolLengthJob.Schedule(matchingJobHandle);
            lastSystemState.currentSymbols.RegisterDependencyOnData(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

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
                totalSymbolCount.Dispose();
                totalSymbolParameterCount.Dispose();
                globalParamNative.Dispose();
                tmpParameterMemory.Dispose();
                matchSingletonData.Dispose();
                if (branchingCache.IsCreated) branchingCache.Dispose();
            }
            
            
            
            currentJobHandle.Complete();

            var totalNewSymbolSize = totalSymbolCount[0];
            var totalNewParamSize = totalSymbolParameterCount[0];
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();

            var nextCompletable =  new LSystemSymbolReplacementCompletable(
                randResult,
                lastSystemState,
                totalNewSymbolSize,
                totalNewParamSize,
                globalParamNative,
                tmpParameterMemory,
                matchSingletonData,
                nativeData,
                branchingCache,
                customSymbols);
            return await nextCompletable.ToUniTask(forceSynchronous, cancel);
        }
    }


    /// <summary>
    /// Step 2. match all rules which could possibly match, without checking the conditional expressions
    /// </summary>
    [BurstCompile]
    public struct RuleCompleteMatchJob : IJobParallelForBatch
    {
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public SymbolString<float> sourceData;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> tmpParameterMemory;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<OperatorDefinition> globalOperatorData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<RuleOutcome.Blittable> outcomes;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> globalParams;

        [ReadOnly]
        public NativeOrderedMultiDictionary<BasicRule.Blittable> blittableRulesByTargetSymbol;

        [ReadOnly]
        public SymbolStringBranchingCache branchingCache;

        public uint seed;

        public void Execute(int startIndex, int batchSize)
        {
            var forwardsMatchHelperStack = new TmpNativeStack<SymbolStringBranchingCache.BranchEventData>(5);
            var rnd = LSystemStepper.RandomFromIndexAndSeed(((uint)startIndex) + 1, seed);
            for (int i = 0; i < batchSize; i++)
            {
                ExecuteAtIndex(i + startIndex, forwardsMatchHelperStack, ref rnd);
            }
        }
        private void ExecuteAtIndex(
            int indexInSymbols,
            TmpNativeStack<SymbolStringBranchingCache.BranchEventData> helperStack,
            ref Unity.Mathematics.Random random)
        {
            var matchSingleton = matchSingletonData[indexInSymbols];
            if (matchSingleton.is_trivial)
            {
                // if match is trivial, then no parameters are captured. the rest of the algo will read directly from the source index
                //  and no transformation will take place.
                return;
            }

            var symbol = sourceData.symbols[indexInSymbols];

            if (!blittableRulesByTargetSymbol.TryGetValue(symbol, out var ruleIndexing) || ruleIndexing.length <= 0)
            {
                matchSingleton.error_code = LSystemMatchErrorCode.TrivialSymbolNotIndicatedAtMatchTime;
                matchSingletonData[indexInSymbols] = matchSingleton;
                return;
            }

            var anyRuleMatched = false;
            var currentIndexInParameterMemory = matchSingleton.tmp_parameter_memory_space.index;
            for (byte i = 0; i < ruleIndexing.length; i++)
            {
                var rule = blittableRulesByTargetSymbol[ruleIndexing, i];
                var success = rule.PreMatchCapturedParametersWithoutConditional(
                    branchingCache,
                    sourceData,
                    indexInSymbols,
                    tmpParameterMemory,
                    currentIndexInParameterMemory,
                    ref matchSingleton,
                    helperStack,
                    globalParams,
                    globalOperatorData,
                    ref random,
                    outcomes
                    );
                if (success)
                {
                    anyRuleMatched = true;
                    matchSingleton.matched_rule_index_in_possible = i;
                    break;
                }
            }
            if (anyRuleMatched == false)
            {
                matchSingleton.is_trivial = true;
            }
            matchSingletonData[indexInSymbols] = matchSingleton;
        }
    }

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
