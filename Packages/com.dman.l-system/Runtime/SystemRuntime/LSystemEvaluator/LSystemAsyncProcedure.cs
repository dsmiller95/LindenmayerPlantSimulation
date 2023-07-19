using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.CustomRules.Diffusion;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    public class LSystemAsyncProcedure
    {
        public static async UniTask<LSystemState<float>> Run(
            LSystemState<float> lastSystemState,
            DependencyTracker<SystemLevelRuleNativeData> nativeData,
            float[] globalParameters,
            ISet<int>[] includedCharactersByRuleIndex,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency,
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            JobHandle currentJobHandle;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("Parameter counts");

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            using var matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(
                lastSystemState.currentSymbols.Data.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            using var branchingCache = new SymbolStringBranchingCache(
                customSymbols.branchOpenSymbol,
                customSymbols.branchCloseSymbol,
                includedCharactersByRuleIndex,
                nativeData.Data);
            int paramTotal;
            {
                using var parameterTotalSum = new NativeArray<int>(1, Allocator.TempJob);
                UnityEngine.Profiling.Profiler.EndSample();

                var memorySizeJob = new SymbolStringMemoryRequirementsJob
                {
                    matchSingletonData = matchSingletonData,
                    memoryRequirementsPerSymbol = nativeData.Data.maxParameterMemoryRequirementsPerSymbol,
                    parameterTotalSum = parameterTotalSum,
                    sourceSymbolString = lastSystemState.currentSymbols.Data
                };

                currentJobHandle = memorySizeJob.Schedule();
                lastSystemState.currentSymbols.RegisterDependencyOnData(currentJobHandle);
                nativeData.RegisterDependencyOnData(currentJobHandle);
                UnityEngine.Profiling.Profiler.EndSample();

                // 2.1
                UnityEngine.Profiling.Profiler.BeginSample("branch cache");
                branchingCache.BuildJumpIndexesFromSymbols(lastSystemState.currentSymbols);
                UnityEngine.Profiling.Profiler.EndSample();

                await AwaitLSystemJob(
                    currentJobHandle,
                    forceSynchronous,
                    cancel);

                paramTotal = parameterTotalSum[0];
            }

            var randResult = lastSystemState.randomProvider;

            int totalNewSymbolSize;
            int totalNewParamSize;
            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            using var tmpParameterMemory =
                new NativeArray<float>(paramTotal, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();
            {
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
                using var totalSymbolCount = new NativeArray<int>(1, Allocator.Persistent);
                using var totalSymbolParameterCount = new NativeArray<int>(1, Allocator.Persistent);
                UnityEngine.Profiling.Profiler.EndSample();

                var totalSymbolLengthJob = new RuleReplacementSizeJob
                {
                    matchSingletonData = matchSingletonData,
                    totalResultSymbolCount = totalSymbolCount,
                    totalResultParameterCount = totalSymbolParameterCount,
                    sourceData = lastSystemState.currentSymbols.Data,
                    customSymbols = customSymbols
                };
                currentJobHandle = totalSymbolLengthJob.Schedule(matchingJobHandle);
                lastSystemState.currentSymbols.RegisterDependencyOnData(currentJobHandle);
                nativeData.RegisterDependencyOnData(currentJobHandle);

                UnityEngine.Profiling.Profiler.EndSample();
                
                await AwaitLSystemJob(
                    currentJobHandle,
                    forceSynchronous,
                    cancel);

                totalNewSymbolSize = totalSymbolCount[0];
                totalNewParamSize = totalSymbolParameterCount[0];
            }

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();

            var hasImmatureSymbols = false;
            uint maxUniqueOrganIds;

            {
                // 5
                UnityEngine.Profiling.Profiler.BeginSample("generating replacements");

                var replacementJob = new RuleReplacementJob
                {
                    globalParametersArray = globalParamNative,

                    parameterMatchMemory = tmpParameterMemory,
                    matchSingletonData = matchSingletonData,

                    sourceData = lastSystemState.currentSymbols.Data,
                    structExpressionSpace = nativeData.Data.structExpressionMemorySpace,
                    globalOperatorData = nativeData.Data.dynamicOperatorMemory,
                    replacementSymbolData = nativeData.Data.replacementsSymbolMemorySpace,
                    outcomeData = nativeData.Data.ruleOutcomeMemorySpace,

                    targetData = target,
                    blittableRulesByTargetSymbol = nativeData.Data.blittableRulesByTargetSymbol,
                    branchingCache = branchingCache,
                    customSymbols = customSymbols
                };

                currentJobHandle = replacementJob.Schedule(
                    matchSingletonData.Length,
                    100
                );

#if !RUST_SUBSYSTEM
                using var diffusionHelper = customSymbols.hasDiffusion ?
                    new DiffusionWorkingDataPack(10, 5, 2, customSymbols, Allocator.TempJob) :
                    default;
#endif

                if (customSymbols.hasDiffusion && !customSymbols.independentDiffusionUpdate)
                {
                    var diffusionJob = new ParallelDiffusionReplacementJob
                    {
                        matchSingletonData = matchSingletonData,
                        sourceData = lastSystemState.currentSymbols.Data,
                        targetData = target,
                        customSymbols = customSymbols,
#if !RUST_SUBSYSTEM
                        working = diffusionHelper
#endif
                    };
                    currentJobHandle = JobHandle.CombineDependencies(
                        currentJobHandle,
                        diffusionJob.Schedule()
                    );
                }

                // only parameter modifications beyond this point
                lastSystemState.currentSymbols.RegisterDependencyOnData(currentJobHandle);
                nativeData.RegisterDependencyOnData(currentJobHandle);

                var (isAssignmentJob, maxIdReached) =
                    ScheduleIdAssignmentJob(currentJobHandle, customSymbols, lastSystemState, target);
                using var _ = maxIdReached;
                
                var (immaturityJob, isImmature) = MaybeScheduleImmaturityJob(currentJobHandle, nativeData, target);

                currentJobHandle = JobHandle.CombineDependencies(
                    JobHandle.CombineDependencies(
                        isAssignmentJob,
                        MaybeScheduleIndependentDiffusion(
                            currentJobHandle,
                            customSymbols,
#if !RUST_SUBSYSTEM
                            diffusionHelper,
#endif
                            target)
                    ),
                    JobHandle.CombineDependencies(
                        MaybeScheduleAutophagyJob(currentJobHandle, customSymbols, target),
                        immaturityJob
                    ));

                UnityEngine.Profiling.Profiler.EndSample();


                try
                {
                    await AwaitLSystemJob(
                        currentJobHandle,
                        forceSynchronous,
                        cancel);
                }
                catch (Exception)
                {
                    target.Dispose();
                    if (isImmature.IsCreated) isImmature.Dispose();
                    throw;
                }
                
                if (isImmature.IsCreated)
                {
                    hasImmatureSymbols = isImmature[0];
                    isImmature.Dispose();
                }
                
                maxUniqueOrganIds = maxIdReached[0];
            }

            var newResult = new LSystemState<float>
            {
                randomProvider = randResult,
                currentSymbols = new DependencyTracker<SymbolString<float>>(target),
                maxUniqueOrganIds = maxUniqueOrganIds,
                hasImmatureSymbols = hasImmatureSymbols,
                firstUniqueOrganId = lastSystemState.firstUniqueOrganId,
                uniquePlantId = lastSystemState.uniquePlantId,
            };

            return newResult;
        }
        
        private static JobHandle MaybeScheduleIndependentDiffusion(
            JobHandle dependency,
            CustomRuleSymbols customSymbols,
#if !RUST_SUBSYSTEM
            DiffusionWorkingDataPack diffusionHelper,
#endif
            SymbolString<float> targetString)
        {
            // diffusion is only dependent on the target symbol data. don't need to register as dependent on native data/source symbols
            if (customSymbols.hasDiffusion && customSymbols.independentDiffusionUpdate)
            {
                var diffusionJob = new IndependentDiffusionReplacementJob
                {
                    inPlaceSymbols = targetString,
                    customSymbols = customSymbols,
#if !RUST_SUBSYSTEM
                    working = diffusionHelper
#endif
                };
                dependency = diffusionJob.Schedule(dependency);
            }
            return dependency;
        }
        private static (JobHandle, NativeArray<uint>) ScheduleIdAssignmentJob(
            JobHandle dependency, 
            CustomRuleSymbols customSymbols,
            LSystemState<float> lastSystemState,
            SymbolString<float> targetString)
        {
            // identity assignment job is not dependent on the source string or any other native data. can skip assigning it as a dependent
            var maxIdReached = new NativeArray<uint>(1, Allocator.TempJob);
            var identityAssignmentJob = new IdentityAssignmentPostProcessRule
            {
                targetData = targetString,
                maxIdentityId = maxIdReached,
                customSymbols = customSymbols,
                lastMaxIdReached = lastSystemState.maxUniqueOrganIds,
                uniquePlantId = lastSystemState.uniquePlantId,
                originOfUniqueIndexes = lastSystemState.firstUniqueOrganId,
            };
            return (identityAssignmentJob.Schedule(dependency), maxIdReached);
        }
        private static JobHandle MaybeScheduleAutophagyJob(
            JobHandle dependency,
            CustomRuleSymbols customSymbols, 
            SymbolString<float> targetString)
        {
            // autophagy is only dependent on the source string. don't need to register as dependent on native data/source symbols
            if (customSymbols.hasAutophagy)
            {
                var helperStack = new TmpNativeStack<AutophagyPostProcess.BranchIdentity>(10, Allocator.TempJob);
                var autophagicJob = new AutophagyPostProcess
                {
                    symbols = targetString,
                    lastIdentityStack = helperStack,
                    customSymbols = customSymbols
                };

                dependency = autophagicJob.Schedule(dependency);
                dependency = helperStack.Dispose(dependency);
            }
            return dependency;
        }
        private static (JobHandle, NativeArray<bool>) MaybeScheduleImmaturityJob(
            JobHandle dependency,
            DependencyTracker<SystemLevelRuleNativeData> nativeData, 
            SymbolString<float> targetString)
        {
            NativeArray<bool> isImmature = default;
            if (nativeData.Data.immaturityMarkerSymbols.IsCreated)
            {
                isImmature = new NativeArray<bool>(1, Allocator.TempJob);
                var immaturityJob = new NativeArrayMultiContainsJob
                {
                    symbols = targetString.symbols,
                    symbolsToCheckFor = nativeData.Data.immaturityMarkerSymbols,
                    doesContainSymbols = isImmature
                };
                dependency = immaturityJob.Schedule(dependency);
                nativeData.RegisterDependencyOnData(dependency);
            }
            return (dependency, isImmature);
        }
        
        
        private static async UniTask AwaitLSystemJob(
            JobHandle handle,
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            using var cancelJobSource =
                CancellationTokenSource.CreateLinkedTokenSource(forceSynchronous, cancel);
            var cancelled = await handle.AwaitCompleteImmediateOnCancel(
                cancelJobSource.Token,
                LSystemJobExecutionConfig.Instance.forceUpdates,
                3);
            if (cancelled && cancel.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
        }
    }
}
