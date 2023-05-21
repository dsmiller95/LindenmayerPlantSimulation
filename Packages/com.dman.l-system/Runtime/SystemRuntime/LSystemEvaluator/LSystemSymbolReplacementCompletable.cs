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
    public class LSystemSymbolReplacementCompletable : ICompletable<LSystemState<float>>
    {
#if UNITY_EDITOR
        public string TaskDescription => "L System symbol replacements";
#endif

        public LSystemState<float> lastSystemState;
        public Unity.Mathematics.Random randResult;

        /////////////// things owned by this step /////////
        private SymbolString<float> target;
        public SymbolStringBranchingCache branchingCache;
        private NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        private DiffusionWorkingDataPack diffusionHelper;
        private NativeArray<uint> maxIdReached;
        private NativeArray<bool> isImmature;

        /////////////// l-system native data /////////
        public DependencyTracker<SystemLevelRuleNativeData> nativeData;

        public JobHandle currentJobHandle { get; private set; }

        public LSystemSymbolReplacementCompletable(
            Unity.Mathematics.Random randResult,
            LSystemState<float> lastSystemState,
            int totalNewSymbolSize,
            int totalNewParamSize,
            NativeArray<float> globalParamNative,
            NativeArray<float> tmpParameterMemory,
            NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
            DependencyTracker<SystemLevelRuleNativeData> nativeData,
            SymbolStringBranchingCache branchingCache,
            CustomRuleSymbols customSymbols)
        {
            this.lastSystemState = lastSystemState;
            this.matchSingletonData = matchSingletonData;
            this.branchingCache = branchingCache;
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();

            this.randResult = randResult;

            this.nativeData = nativeData;

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

            currentJobHandle = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    ScheduleIdAssignmentJob(currentJobHandle, customSymbols, lastSystemState),
                    ScheduleIndependentDiffusion(currentJobHandle, customSymbols)
                ),
                JobHandle.CombineDependencies(
                    ScheduleAutophagyJob(currentJobHandle, customSymbols),
                    ScheduleImmaturityJob(currentJobHandle)
                ));

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private JobHandle ScheduleIndependentDiffusion(JobHandle dependency, CustomRuleSymbols customSymbols)
        {
            // diffusion is only dependent on the target symbol data. don't need to register as dependent on native data/source symbols
            if (customSymbols.hasDiffusion && customSymbols.independentDiffusionUpdate)
            {
                diffusionHelper = new DiffusionWorkingDataPack(10, 5, 2, customSymbols, Allocator.TempJob);
                var diffusionJob = new IndependentDiffusionReplacementJob
                {
                    inPlaceSymbols = target,
                    customSymbols = customSymbols,
                    working = diffusionHelper
                };
                dependency = diffusionJob.Schedule(dependency);
            }
            return dependency;
        }
        private JobHandle ScheduleIdAssignmentJob(
            JobHandle dependency, 
            CustomRuleSymbols customSymbols,
            LSystemState<float> lastSystemState)
        {
            // identity assignment job is not dependent on the source string or any other native data. can skip assigning it as a dependent
            maxIdReached = new NativeArray<uint>(1, Allocator.TempJob);
            var identityAssignmentJob = new IdentityAssignmentPostProcessRule
            {
                targetData = target,
                maxIdentityId = maxIdReached,
                customSymbols = customSymbols,
                lastMaxIdReached = lastSystemState.maxUniqueOrganIds,
                uniquePlantId = lastSystemState.uniquePlantId,
                originOfUniqueIndexes = lastSystemState.firstUniqueOrganId,
            };
            return identityAssignmentJob.Schedule(dependency);
        }
        private JobHandle ScheduleAutophagyJob(JobHandle dependency, CustomRuleSymbols customSymbols)
        {
            // autophagy is only dependent on the source string. don't need to register as dependent on native data/source symbols
            if (customSymbols.hasAutophagy)
            {
                var helperStack = new TmpNativeStack<AutophagyPostProcess.BranchIdentity>(10, Allocator.TempJob);
                var autophagicJob = new AutophagyPostProcess
                {
                    symbols = target,
                    lastIdentityStack = helperStack,
                    customSymbols = customSymbols
                };

                dependency = autophagicJob.Schedule(dependency);
                dependency = helperStack.Dispose(dependency);
            }
            return dependency;
        }
        private JobHandle ScheduleImmaturityJob(JobHandle dependency)
        {
            if (nativeData.Data.immaturityMarkerSymbols.IsCreated)
            {
                isImmature = new NativeArray<bool>(1, Allocator.TempJob);
                var immaturityJob = new NativeArrayMultiContainsJob
                {
                    symbols = target.symbols,
                    symbolsToCheckFor = nativeData.Data.immaturityMarkerSymbols,
                    doesContainSymbols = isImmature
                };
                dependency = immaturityJob.Schedule(dependency);
                nativeData.RegisterDependencyOnData(dependency);
            }
            return dependency;
        }

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();
            branchingCache.Dispose();
            matchSingletonData.Dispose();
            if (diffusionHelper.IsCreated) diffusionHelper.Dispose();

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
            currentJobHandle.Complete();

            maxIdReached.Dispose();
            target.Dispose();
            matchSingletonData.Dispose();
            if (diffusionHelper.IsCreated) diffusionHelper.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
            if (isImmature.IsCreated) isImmature.Dispose();
            return inputDeps;
        }

        public void Dispose()
        {
            currentJobHandle.Complete();

            maxIdReached.Dispose();
            target.Dispose();
            matchSingletonData.Dispose();
            if (diffusionHelper.IsCreated) diffusionHelper.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
            if (isImmature.IsCreated) isImmature.Dispose();
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
