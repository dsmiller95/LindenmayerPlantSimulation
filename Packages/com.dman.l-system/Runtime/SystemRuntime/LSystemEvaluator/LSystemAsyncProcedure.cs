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
        private readonly CancellationToken _forceSynchronous;
        private readonly CancellationToken _cancel;

        public LSystemAsyncProcedure(
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            _forceSynchronous = forceSynchronous;
            _cancel = cancel;
        }
        
        public async UniTask<LSystemState<float>> Run(
            LSystemState<float> lastSystemState,
            DependencyTracker<SystemLevelRuleNativeData> nativeData,
            float[] globalParameters,
            ISet<int>[] includedCharactersByRuleIndex,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency)
        {
            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            using var branchingCache = new SymbolStringBranchingCache(
                customSymbols.branchOpenSymbol,
                customSymbols.branchCloseSymbol,
                includedCharactersByRuleIndex,
                nativeData.Data);

            using var singletonDataPack = new MatchSingletonsDataPacket(lastSystemState, nativeData);
            UnityEngine.Profiling.Profiler.EndSample();
            
            var paramTotalTask = CountParameterTotals(singletonDataPack);

            // 2.1
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            branchingCache.BuildJumpIndexesFromSymbols(lastSystemState.currentSymbols);
            UnityEngine.Profiling.Profiler.EndSample();
            
            var paramTotal = await paramTotalTask;
            var randResult = lastSystemState.randomProvider;

            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            using var matchDataPack = new MatchAndWriteWorkingMemoryDataPacket(
                branchingCache,
                paramTotal,
                globalParameters);
            UnityEngine.Profiling.Profiler.EndSample();

            var (totalNewSymbolSize, totalNewParamSize) = await PerformLSystemMatch(
                singletonDataPack,
                matchDataPack,
                customSymbols,
                parameterModificationJobDependency,
                randResult.NextUInt());

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();
            
            var (hasImmatureSymbols, maxUniqueOrganIds) = await PerformSymbolReplacement(
                singletonDataPack,
                matchDataPack,
                customSymbols,
                target);

            return new LSystemState<float>
            {
                randomProvider = randResult,
                currentSymbols = new DependencyTracker<SymbolString<float>>(target),
                maxUniqueOrganIds = maxUniqueOrganIds,
                hasImmatureSymbols = hasImmatureSymbols,
                firstUniqueOrganId = lastSystemState.firstUniqueOrganId,
                uniquePlantId = lastSystemState.uniquePlantId,
            };
        }

        private struct MatchSingletonsDataPacket : IDisposable
        {
            public LSystemState<float> lastSystemState;
            public DependencyTracker<SystemLevelRuleNativeData> nativeData;
            public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;
            
            public SystemLevelRuleNativeData systemData => nativeData.Data;
            public SymbolString<float> symbols => lastSystemState.currentSymbols.Data;

            public int Length => symbols.Length;

            public MatchSingletonsDataPacket(
                LSystemState<float> lastSystemState,
                DependencyTracker<SystemLevelRuleNativeData> nativeData,
                Allocator allocator = Allocator.Persistent
            )
            {
                this.lastSystemState = lastSystemState;
                this.nativeData = nativeData;
                
                matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(
                    lastSystemState.currentSymbols.Data.Length, 
                    allocator,
                    NativeArrayOptions.UninitializedMemory);
            }
            
            public void Dispose()
            {
                matchSingletonData.Dispose();
            }

            public void RegisterDependency(JobHandle deps)
            {
                lastSystemState.currentSymbols.RegisterDependencyOnData(deps);
                nativeData.RegisterDependencyOnData(deps);
            }
            
        }
        private async Task<int> CountParameterTotals(MatchSingletonsDataPacket singletonDataPack)
        {
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            using var parameterTotalSum = new NativeArray<int>(1, Allocator.TempJob);

            var memorySizeJob = new SymbolStringMemoryRequirementsJob
            {
                matchSingletonData = singletonDataPack.matchSingletonData,
                memoryRequirementsPerSymbol = singletonDataPack.systemData.maxParameterMemoryRequirementsPerSymbol,
                parameterTotalSum = parameterTotalSum,
                sourceSymbolString = singletonDataPack.symbols
            };

            var currentJobHandle = memorySizeJob.Schedule();
            singletonDataPack.RegisterDependency(currentJobHandle);
            UnityEngine.Profiling.Profiler.EndSample();

            await AwaitLSystemJob(currentJobHandle);

            return parameterTotalSum[0];
        }

        private struct MatchAndWriteWorkingMemoryDataPacket : IDisposable
        {
            public NativeArray<float> globalParamNative;
            public NativeArray<float> tmpParameterMemory;
            public SymbolStringBranchingCache branchingCache;

            public MatchAndWriteWorkingMemoryDataPacket(
                SymbolStringBranchingCache branchingCache,
                int totalParameters,
                float[] globalParameters,
                Allocator allocator = Allocator.Persistent)
            {
                this.branchingCache = branchingCache;
                tmpParameterMemory =
                    new NativeArray<float>(totalParameters, allocator, NativeArrayOptions.UninitializedMemory);
                globalParamNative = new NativeArray<float>(globalParameters, allocator);
            }

            public void Dispose()
            {
                tmpParameterMemory.Dispose();
                globalParamNative.Dispose();
            }
        }
        private async Task<(int symbolSize, int paramSize)> PerformLSystemMatch(
            MatchSingletonsDataPacket singletonDataPack,
            MatchAndWriteWorkingMemoryDataPacket matchDataPack,
            CustomRuleSymbols customSymbols,
            JobHandle parameterModificationJobDependency,
            uint randomSeed)
        {
            // 2.
            UnityEngine.Profiling.Profiler.BeginSample("matching");
            
            var prematchJob = new RuleCompleteMatchJob
            {
                matchSingletonData = singletonDataPack.matchSingletonData,

                sourceData = singletonDataPack.symbols,
                tmpParameterMemory = matchDataPack.tmpParameterMemory,

                globalOperatorData = singletonDataPack.systemData.dynamicOperatorMemory,
                outcomes = singletonDataPack.systemData.ruleOutcomeMemorySpace,
                globalParams = matchDataPack.globalParamNative,

                blittableRulesByTargetSymbol = singletonDataPack.systemData.blittableRulesByTargetSymbol,
                branchingCache = matchDataPack.branchingCache,
                seed = randomSeed
            };

            var currentJobHandle = prematchJob.ScheduleBatch(
                singletonDataPack.Length,
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
                matchSingletonData = singletonDataPack.matchSingletonData,
                sourceData = singletonDataPack.symbols,
                
                totalResultSymbolCount = totalSymbolCount,
                totalResultParameterCount = totalSymbolParameterCount,
                customSymbols = customSymbols
            };
            currentJobHandle = totalSymbolLengthJob.Schedule(currentJobHandle);
            singletonDataPack.RegisterDependency(currentJobHandle);

            UnityEngine.Profiling.Profiler.EndSample();

            await AwaitLSystemJob(currentJobHandle);

            return (totalSymbolCount[0], totalSymbolParameterCount[0]);
        }

        private async Task<(bool hasImmatureSymbols, uint maxUniqueOrganIds)> PerformSymbolReplacement(
            MatchSingletonsDataPacket singletonDataPack,
            MatchAndWriteWorkingMemoryDataPacket matchDataPack,
            CustomRuleSymbols customSymbols,
            SymbolString<float> target)
        {
            // 5
            UnityEngine.Profiling.Profiler.BeginSample("generating replacements");

            var replacementJob = new RuleReplacementJob
            {
                globalParametersArray = matchDataPack.globalParamNative,

                parameterMatchMemory = matchDataPack.tmpParameterMemory,
                matchSingletonData = singletonDataPack.matchSingletonData,

                sourceData = singletonDataPack.symbols,
                structExpressionSpace = singletonDataPack.systemData.structExpressionMemorySpace,
                globalOperatorData = singletonDataPack.systemData.dynamicOperatorMemory,
                replacementSymbolData = singletonDataPack.systemData.replacementsSymbolMemorySpace,
                outcomeData = singletonDataPack.systemData.ruleOutcomeMemorySpace,

                targetData = target,
                blittableRulesByTargetSymbol = singletonDataPack.systemData.blittableRulesByTargetSymbol,
                branchingCache = matchDataPack.branchingCache,
                customSymbols = customSymbols
            };

            var currentJobHandle = replacementJob.Schedule(
                singletonDataPack.Length,
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
                    matchSingletonData = singletonDataPack.matchSingletonData,
                    sourceData = singletonDataPack.symbols,
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
            singletonDataPack.RegisterDependency(currentJobHandle);

            var (isAssignmentJob, maxIdReached) =
                ScheduleIdAssignmentJob(currentJobHandle, customSymbols, singletonDataPack.lastSystemState, target);
            using var _ = maxIdReached;

            var independentDiffusionJob = MaybeScheduleIndependentDiffusion(
                currentJobHandle,
                customSymbols,
#if !RUST_SUBSYSTEM
                    diffusionHelper,
#endif
                target);

            var autophagyJob = MaybeScheduleAutophagyJob(currentJobHandle, customSymbols, target);

            var (immaturityJob, isImmature) = MaybeScheduleImmaturityJob(currentJobHandle, singletonDataPack.nativeData, target);
            using NativeArrayNativeDisposableAdapter<bool> __ = isImmature;

            currentJobHandle = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    isAssignmentJob,
                    independentDiffusionJob
                ),
                JobHandle.CombineDependencies(
                    autophagyJob,
                    immaturityJob
                ));

            UnityEngine.Profiling.Profiler.EndSample();


            try
            {
                await AwaitLSystemJob(currentJobHandle);
            }
            catch (Exception)
            {
                target.Dispose();
                throw;
            }

            return (
                isImmature.IsCreated && isImmature[0], 
                maxIdReached[0]);
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
        
        
        private async UniTask AwaitLSystemJob(JobHandle handle)
        {
            using var cancelJobSource =
                CancellationTokenSource.CreateLinkedTokenSource(_forceSynchronous, _cancel);
            var cancelled = await handle.AwaitCompleteImmediateOnCancel(
                cancelJobSource.Token,
                LSystemJobExecutionConfig.Instance.forceUpdates,
                3);
            if (cancelled && _cancel.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
        }

    }
}
