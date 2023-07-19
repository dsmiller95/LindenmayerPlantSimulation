﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.CustomRules.Diffusion;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    public class LSystemSymbolReplacementCompletable
    {
        public static async UniTask<LSystemState<float>> Run(
            Unity.Mathematics.Random randResult,
            LSystemState<float> lastSystemState,
            int totalNewSymbolSize,
            int totalNewParamSize,
            NativeArray<float> globalParamNative,
            NativeArray<float> tmpParameterMemory,
            NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
            DependencyTracker<SystemLevelRuleNativeData> nativeData,
            SymbolStringBranchingCache branchingCache,
            CustomRuleSymbols customSymbols,
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();

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

            var currentJobHandle = replacementJob.Schedule(
                    matchSingletonData.Length,
                    100
                );

            if (customSymbols.hasDiffusion && !customSymbols.independentDiffusionUpdate)
            {
#if !RUST_SUBSYSTEM
                diffusionHelper = new DiffusionWorkingDataPack(10, 5, 2, customSymbols, Allocator.TempJob);
#endif
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

            var (isAssignmentJob, maxIdReached) = ScheduleIdAssignmentJob(currentJobHandle, customSymbols, lastSystemState, target);

            var (immaturityJob, isImmature) = ScheduleImmaturityJob(currentJobHandle, nativeData, target);
            
            currentJobHandle = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    isAssignmentJob,
                    ScheduleIndependentDiffusion(currentJobHandle, customSymbols, target)
                ),
                JobHandle.CombineDependencies(
                    ScheduleAutophagyJob(currentJobHandle, customSymbols, target),
                    immaturityJob
                ));

            UnityEngine.Profiling.Profiler.EndSample();
            
            
            
            using var cancelJobSource = CancellationTokenSource.CreateLinkedTokenSource(forceSynchronous, cancel);
            try
            {
                var cancelled = await currentJobHandle.AwaitCompleteImmediateOnCancel(
                    cancelJobSource.Token,
                    LSystemJobExecutionConfig.Instance.forceUpdates,
                    3);
                if (cancelled && cancel.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            catch(Exception)
            {
                currentJobHandle.Complete();

                maxIdReached.Dispose();
                target.Dispose();
                matchSingletonData.Dispose();
#if !RUST_SUBSYSTEM
            if (diffusionHelper.IsCreated) diffusionHelper.Dispose();
#endif
                if (branchingCache.IsCreated) branchingCache.Dispose();
                if (isImmature.IsCreated) isImmature.Dispose();
            }

            
            
            
            currentJobHandle.Complete();
            branchingCache.Dispose();
            matchSingletonData.Dispose();
#if !RUST_SUBSYSTEM
            if (diffusionHelper.IsCreated) diffusionHelper.Dispose();
#endif

            var hasImmatureSymbols = false;

            if (isImmature.IsCreated)
            {
                hasImmatureSymbols = isImmature[0];
                isImmature.Dispose();
            }

            var newResult = new LSystemState<float>
            {
                randomProvider = randResult,
                currentSymbols = new DependencyTracker<SymbolString<float>>(target),
                maxUniqueOrganIds = maxIdReached[0],
                hasImmatureSymbols = hasImmatureSymbols,
                firstUniqueOrganId = lastSystemState.firstUniqueOrganId,
                uniquePlantId = lastSystemState.uniquePlantId,
            };

            maxIdReached.Dispose();
            return newResult;
        }
        
        private static JobHandle ScheduleIndependentDiffusion(
            JobHandle dependency,
            CustomRuleSymbols customSymbols,
            SymbolString<float> targetString)
        {
            // diffusion is only dependent on the target symbol data. don't need to register as dependent on native data/source symbols
            if (customSymbols.hasDiffusion && customSymbols.independentDiffusionUpdate)
            {
#if !RUST_SUBSYSTEM
                diffusionHelper = new DiffusionWorkingDataPack(10, 5, 2, customSymbols, Allocator.TempJob);
#endif
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
        private static JobHandle ScheduleAutophagyJob(
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
        private static (JobHandle, NativeArray<bool>) ScheduleImmaturityJob(
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
    }

    [BurstCompile]
    public struct RuleReplacementJob : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> globalParametersArray;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> parameterMatchMemory;

        [ReadOnly]
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<RuleOutcome.Blittable> outcomeData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<ReplacementSymbolGenerator.Blittable> replacementSymbolData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<StructExpression> structExpressionSpace;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<OperatorDefinition> globalOperatorData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public SymbolString<float> sourceData;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction] // disable all safety to allow parallel writes
        public SymbolString<float> targetData;

        [ReadOnly]
        public NativeOrderedMultiDictionary<BasicRule.Blittable> blittableRulesByTargetSymbol;
        [ReadOnly]
        public SymbolStringBranchingCache branchingCache;

        public CustomRuleSymbols customSymbols;

        public void Execute(int indexInSymbols)
        {
            var matchSingleton = matchSingletonData[indexInSymbols];
            var symbol = sourceData.symbols[indexInSymbols];
            if (matchSingleton.is_trivial)
            {
                var targetIndex = matchSingleton.replacement_symbol_indexing.index;
                // check for custom rules
                if (customSymbols.hasDiffusion && !customSymbols.independentDiffusionUpdate)
                {
                    // let the diffusion library handle these updates. only if diffusion is happening in parallel
                    if (symbol == customSymbols.diffusionNode || symbol == customSymbols.diffusionAmount)
                    {
                        return;
                    }
                }

                // match is trivial. just copy the existing symbol and parameters over, nothing else.
                targetData.symbols[targetIndex] = symbol;
                var sourceParamIndexer = sourceData.parameters[indexInSymbols];
                var targetDataIndexer = new JaggedIndexing
                {
                    index = matchSingleton.replacement_parameter_indexing.index,
                    length = sourceParamIndexer.length
                };
                targetData.parameters[targetIndex] = targetDataIndexer;
                // when trivial, copy out of the source param array directly. As opposed to reading parameters out of the parameterMatchMemory when evaluating
                //      a non-trivial match
                for (int i = 0; i < sourceParamIndexer.length; i++)
                {
                    targetData.parameters[targetDataIndexer, i] = sourceData.parameters[sourceParamIndexer, i];
                }
                return;
            }

            if (!blittableRulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList.length <= 0)
            {
                //throw new System.Exception(LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME.ToString());

                return;
            }

            var rule = blittableRulesByTargetSymbol[ruleList, matchSingleton.matched_rule_index_in_possible];

            rule.WriteReplacementSymbols(
                globalParametersArray,
                parameterMatchMemory,
                targetData,
                matchSingleton,
                globalOperatorData,
                replacementSymbolData,
                outcomeData,
                structExpressionSpace
                );
        }
    }
}
