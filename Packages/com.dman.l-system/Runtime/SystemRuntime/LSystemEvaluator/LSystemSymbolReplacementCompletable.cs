﻿using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
{
    public class LSystemSymbolReplacementCompletable : ICompletable<LSystemState<float>>
    {
        public DependencyTracker<SymbolString<float>> sourceSymbolString;
        public Unity.Mathematics.Random randResult;

        /////////////// things owned by this step /////////
        private SymbolString<float> target;

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
            DependencyTracker<SystemLevelRuleNativeData> nativeData)
        {
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
                blittableRulesByTargetSymbol = nativeData.Data.blittableRulesByTargetSymbol
            };

            currentJobHandle = replacementJob.Schedule(
                matchSingletonData.Length,
                100
            );
            sourceSymbolString.RegisterDependencyOnData(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();
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
            return inputDeps;
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            target.Dispose();
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
        [DeallocateOnJobCompletion]
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

        [NativeDisableParallelForRestriction]
        public SymbolString<float> targetData;

        [ReadOnly]
        public NativeOrderedMultiDictionary<BasicRule.Blittable> blittableRulesByTargetSymbol;

        public void Execute(int indexInSymbols)
        {
            var matchSingleton = matchSingletonData[indexInSymbols];
            var symbol = sourceData.symbols[indexInSymbols];
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, just copy the symbol over, nothing else.
                var targetIndex = matchSingleton.replacementSymbolIndexing.index;
                targetData.symbols[targetIndex] = symbol;
                var sourceParamIndexer = sourceData.newParameters[indexInSymbols];
                var targetDataIndexer = targetData.newParameters[targetIndex] = new JaggedIndexing
                {
                    index = matchSingleton.replacementParameterIndexing.index,
                    length = sourceParamIndexer.length
                };
                // when trivial, copy out of the source param array directly. As opposed to reading parameters out oof the parameterMatchMemory when evaluating
                //      a non-trivial match
                for (int i = 0; i < sourceParamIndexer.length; i++)
                {
                    targetData.newParameters[targetDataIndexer, i] = sourceData.newParameters[sourceParamIndexer, i];
                }
                return;
            }

            if (!blittableRulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList.length <= 0)
            {
                matchSingleton.errorCode = LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME;
                matchSingletonData[indexInSymbols] = matchSingleton;
                // could recover gracefully. but for now, going to force a failure
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

}
