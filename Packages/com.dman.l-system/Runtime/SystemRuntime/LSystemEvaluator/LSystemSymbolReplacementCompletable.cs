using Dman.LSystem.SystemRuntime.CustomRules;
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
        public DependencyTracker<SymbolString<float>> sourceSymbolString;
        public Unity.Mathematics.Random randResult;

        /////////////// things owned by this step /////////
        private SymbolString<float> target;
        public SymbolStringBranchingCache branchingCache;
        private NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        private DiffusionWorkingDataPack diffusionHelper;

        /////////////// l-system native data /////////
        public DependencyTracker<SystemLevelRuleNativeData> nativeData;

        public JobHandle currentJobHandle { get; private set; }

        public LSystemSymbolReplacementCompletable(
            Unity.Mathematics.Random randResult,
            DependencyTracker<SymbolString<float>> sourceSymbolString,
            int totalNewSymbolSize,
            int totalNewParamSize,
            NativeArray<float> globalParamNative,
            NativeArray<float> tmpParameterMemory,
            NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
            DependencyTracker<SystemLevelRuleNativeData> nativeData,
            SymbolStringBranchingCache branchingCache,
            CustomRuleSymbols customSymbols)
        {
            this.matchSingletonData = matchSingletonData;
            this.branchingCache = branchingCache;
            target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);

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
                blittableRulesByTargetSymbol = nativeData.Data.blittableRulesByTargetSymbol,
                branchingCache = branchingCache,
                customSymbols = customSymbols
            };


            diffusionHelper = new DiffusionWorkingDataPack(10, 5, 2, Allocator.TempJob);

            var diffusionJob = new DiffusionReplacementJob
            {
                matchSingletonData = matchSingletonData,

                sourceData = sourceSymbolString.Data,
                targetData = target,
                branchingCache = branchingCache,
                customSymbols = customSymbols,

                working = diffusionHelper
            };

            // agressively register dependencies, to ensure that if there is a problem
            //  when scheduling any one job, they are still tracked.
            var replacementHandle = replacementJob.Schedule(
                    matchSingletonData.Length,
                    100
                );
            sourceSymbolString.RegisterDependencyOnData(replacementHandle);
            nativeData.RegisterDependencyOnData(replacementHandle);

            var diffusionHandle = diffusionJob.Schedule();
            sourceSymbolString.RegisterDependencyOnData(diffusionHandle);
            nativeData.RegisterDependencyOnData(diffusionHandle);

            currentJobHandle = JobHandle.CombineDependencies(
                replacementHandle,
                diffusionHandle
             );


            sourceSymbolString.RegisterDependencyOnData(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();
            branchingCache.Dispose();
            matchSingletonData.Dispose();
            diffusionHelper.Dispose();
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
            currentJobHandle.Complete();
            target.Dispose();
            matchSingletonData.Dispose();
            diffusionHelper.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
            return inputDeps;
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            target.Dispose();
            matchSingletonData.Dispose();
            diffusionHelper.Dispose();
            if (branchingCache.IsCreated) branchingCache.Dispose();
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
            if (matchSingleton.isTrivial)
            {
                var targetIndex = matchSingleton.replacementSymbolIndexing.index;
                // check for custom rules
                if (customSymbols.hasDiffusion)
                {
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
                    index = matchSingleton.replacementParameterIndexing.index,
                    length = sourceParamIndexer.length
                };
                targetData.parameters[targetIndex] = targetDataIndexer;
                // when trivial, copy out of the source param array directly. As opposed to reading parameters out oof the parameterMatchMemory when evaluating
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

            var rule = blittableRulesByTargetSymbol[ruleList, matchSingleton.matchedRuleIndexInPossible];

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


    public struct DiffusionEdge
    {
        public int nodeAIndex;
        public int nodeBIndex;
    }

    public struct DiffusionNode
    {
        public int indexInTarget;
        public JaggedIndexing targetParameters;

        public int indexInTempAmountList;

        public int totalResourceTypes;
        public float diffusionConstant;
    }


    public struct DiffusionWorkingDataPack : INativeDisposable
    {
        [NativeDisableParallelForRestriction]
        public NativeList<DiffusionEdge> allEdges;
        [NativeDisableParallelForRestriction]
        public NativeList<DiffusionNode> nodes;

        [NativeDisableParallelForRestriction]
        public NativeList<float> nodeMaxCapacities;
        [NativeDisableParallelForRestriction]
        public NativeList<float> nodeAmountsListA;
        [NativeDisableParallelForRestriction]
        public NativeList<float> nodeAmountsListB;

        public DiffusionWorkingDataPack(int estimatedEdges, int estimatedNodes, int estimatedUniqueResources, Allocator allocator = Allocator.TempJob)
        {
            allEdges = new NativeList<DiffusionEdge>(estimatedEdges, allocator);
            nodes = new NativeList<DiffusionNode>(estimatedNodes, allocator);

            nodeMaxCapacities = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
            nodeAmountsListA = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
            nodeAmountsListB = new NativeList<float>(estimatedNodes * estimatedUniqueResources, allocator);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    allEdges.Dispose(inputDeps),
                    nodes.Dispose(inputDeps)
                ),
                JobHandle.CombineDependencies(
                    nodeMaxCapacities.Dispose(inputDeps),
                    nodeAmountsListA.Dispose(inputDeps),
                    nodeAmountsListB.Dispose(inputDeps)
                ));
        }

        public void Dispose()
        {
            allEdges.Dispose();
            nodes.Dispose();
            nodeMaxCapacities.Dispose();
            nodeAmountsListA.Dispose();
            nodeAmountsListB.Dispose();
        }
    }

    [BurstCompile]
    struct DiffusionReplacementJob : IJob
    {
        [ReadOnly]
        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public SymbolString<float> sourceData;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction] // disable all safety to allow parallel writes
        public SymbolString<float> targetData;

        [ReadOnly]
        public SymbolStringBranchingCache branchingCache;

        public DiffusionWorkingDataPack working;

        public CustomRuleSymbols customSymbols;

        public void Execute()
        {
            if (customSymbols.hasDiffusion)
            {
                DiffusionRule.ExtractEdgesAndNodes(sourceData, matchSingletonData, branchingCache, customSymbols, working);

                var latestDataInA = true;
                for (int i = 0; i < customSymbols.diffusionStepsPerStep; i++)
                {
                    DiffusionRule.DiffuseBetween(working, latestDataInA);
                    latestDataInA = !latestDataInA;
                }

                DiffusionRule.ApplyDiffusionResults(targetData, working, customSymbols, latestDataInA);
            }
        }

    }

}
