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
    public class LSystemRuleMatchCompletable : ICompletable<LSystemState<float>>
    {
        public DependencyTracker<SymbolString<float>> sourceSymbolString;
        public Unity.Mathematics.Random randResult;
        public CustomRuleSymbols customSymbols;
        public uint uniqueIDOriginIndex;

        /////////////// things owned by this step /////////
        public NativeArray<int> totalSymbolCount;
        public NativeArray<int> totalSymbolParameterCount;


        /////////////// things transferred to the next step /////////
        public SymbolStringBranchingCache branchingCache;
        public NativeArray<float> globalParamNative;
        public NativeArray<float> tmpParameterMemory;
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        /////////////// l-system native data /////////
        public DependencyTracker<SystemLevelRuleNativeData> nativeData;


        public JobHandle currentJobHandle { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="systemState"></param>
        /// <param name="lSystemNativeData"></param>
        /// <param name="globalParameters"></param>
        /// <param name="maxMemoryRequirementsPerSymbol"></param>
        /// <param name="branchOpenSymbol"></param>
        /// <param name="branchCloseSymbol"></param>
        /// <param name="includedCharactersByRuleIndex"></param>
        /// <param name="customSymbols"></param>
        /// <param name="parameterModificationJobDependency">A dependency on a job which only makes changes to the parameters of the source symbol string.
        ///     the symbols themselves must be constant</param>
        public LSystemRuleMatchCompletable(
            LSystemState<float> systemState,
            DependencyTracker<SystemLevelRuleNativeData> lSystemNativeData,
            float[] globalParameters,
            IDictionary<int, MaxMatchMemoryRequirements> maxMemoryRequirementsPerSymbol,
            int branchOpenSymbol,
            int branchCloseSymbol,
            ISet<int>[] includedCharactersByRuleIndex,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency)
        {
            this.customSymbols = customSymbols;
            uniqueIDOriginIndex = systemState.firstUniqueOrganId;
            randResult = systemState.randomProvider;
            sourceSymbolString = systemState.currentSymbols;
            nativeData = lSystemNativeData;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("Paramter counts");
            matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(sourceSymbolString.Data.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var parameterTotalSum = 0;
            for (int i = 0; i < sourceSymbolString.Data.Length; i++)
            {
                var symbol = sourceSymbolString.Data[i];
                var matchData = new LSystemSingleSymbolMatchData()
                {
                    tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(parameterTotalSum)
                };
                if (maxMemoryRequirementsPerSymbol.TryGetValue(symbol, out var memoryRequirements))
                {
                    parameterTotalSum += memoryRequirements.maxParameters;
                    matchData.isTrivial = false;
                }
                else
                {
                    matchData.isTrivial = true;
                    matchData.tmpParameterMemorySpace.length = 0;
                }
                matchSingletonData[i] = matchData;
            }

            tmpParameterMemory = new NativeArray<float>(parameterTotalSum, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            UnityEngine.Profiling.Profiler.EndSample();

            // 2.
            UnityEngine.Profiling.Profiler.BeginSample("matching");
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            branchingCache = new SymbolStringBranchingCache(
                branchOpenSymbol,
                branchCloseSymbol,
                includedCharactersByRuleIndex,
                nativeData.Data);
            branchingCache.BuildJumpIndexesFromSymbols(sourceSymbolString);
            UnityEngine.Profiling.Profiler.EndSample();

            globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);

            var prematchJob = new RuleCompleteMatchJob
            {
                matchSingletonData = matchSingletonData,

                sourceData = sourceSymbolString.Data,
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

            totalSymbolCount = new NativeArray<int>(1, Allocator.Persistent);
            totalSymbolParameterCount = new NativeArray<int>(1, Allocator.Persistent);

            var totalSymbolLengthJob = new RuleReplacementSizeJob
            {
                matchSingletonData = matchSingletonData,
                totalResultSymbolCount = totalSymbolCount,
                totalResultParameterCount = totalSymbolParameterCount,
                sourceData = sourceSymbolString.Data,
                customSymbols = customSymbols
            };
            currentJobHandle = totalSymbolLengthJob.Schedule(matchingJobHandle);
            sourceSymbolString.RegisterDependencyOnData(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();

            var totalNewSymbolSize = totalSymbolCount[0];
            var totalNewParamSize = totalSymbolParameterCount[0];
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();

            return new LSystemSymbolReplacementCompletable(
                randResult,
                sourceSymbolString,
                totalNewSymbolSize,
                totalNewParamSize,
                globalParamNative,
                tmpParameterMemory,
                matchSingletonData,
                nativeData,
                branchingCache,
                customSymbols,
                uniqueIDOriginIndex)
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
            currentJobHandle.Complete();
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
            currentJobHandle.Complete();
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();
            globalParamNative.Dispose();
            tmpParameterMemory.Dispose();
            matchSingletonData.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
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
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, then no parameters are captured. the rest of the algo will read directly from the source index
                //  and no transformation will take place.
                return;
            }

            var symbol = sourceData.symbols[indexInSymbols];

            if (!blittableRulesByTargetSymbol.TryGetValue(symbol, out var ruleIndexing) || ruleIndexing.length <= 0)
            {
                matchSingleton.errorCode = LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_MATCH_TIME;
                matchSingletonData[indexInSymbols] = matchSingleton;
                return;
            }

            var anyRuleMatched = false;
            var currentIndexInParameterMemory = matchSingleton.tmpParameterMemorySpace.index;
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
                    matchSingleton.matchedRuleIndexInPossible = i;
                    break;
                }
            }
            if (anyRuleMatched == false)
            {
                matchSingleton.isTrivial = true;
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
                singleton.replacementSymbolIndexing.index = totalResultSymbolSize;
                singleton.replacementParameterIndexing.index = totalResultParamSize;
                matchSingletonData[i] = singleton;
                if (!singleton.isTrivial)
                {
                    totalResultSymbolSize += singleton.replacementSymbolIndexing.length;
                    totalResultParamSize += singleton.replacementParameterIndexing.length;
                    continue;
                }
                // custom rules
                if (customSymbols.hasDiffusion && customSymbols.diffusionAmount == sourceData[i])
                {
                    //... do nothing if it matches the custom diffusion symbol.
                    // will copy 0 data over, the symbol dissapears.
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
