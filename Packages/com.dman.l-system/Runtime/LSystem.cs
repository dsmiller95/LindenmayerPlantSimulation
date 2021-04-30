using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem
{
    public static class LSystemBuilder
    {
        /// <summary>
        /// Compile a new L-system from rule text
        /// </summary>
        /// <param name="rules">a list of all of the rules in this L-System</param>
        /// <param name="globalParameters">A list of global parameters.
        ///     The returned LSystem will require a double[] of the same length be passed in to the step function</param>
        /// <returns></returns>
        public static LSystem FloatSystem(
           IEnumerable<string> rules,
           string[] globalParameters = null,
           string ignoredCharacters = "")
        {
            var compiledRules = RuleParser.CompileRules(
                        rules,
                        out var nativeRuleData,
                        globalParameters
                        );

            return new LSystem(
                compiledRules,
                nativeRuleData,
                globalParameters?.Length ?? 0,
                ignoredCharacters: new HashSet<int>(ignoredCharacters.Select(x => (int)x))
            );
        }
    }

    public class LSystemState<T> where T : unmanaged
    {
        public SymbolString<T> currentSymbols { get; set; }
        public Unity.Mathematics.Random randomProvider;
    }

    public class DefaultLSystemState : LSystemState<float>
    {
        public DefaultLSystemState(string axiom, int seed) : this(axiom, (uint)seed)
        { }
        public DefaultLSystemState(string axiom, uint seed = 1)
        {
            currentSymbols = SymbolString<float>.FromString(axiom);
            randomProvider = new Unity.Mathematics.Random(seed);
        }
    }

    public struct MaxMatchMemoryRequirements
    {
        public int maxParameters;
        public int maxPossibleMatches;
    }

    public class LSystem : System.IDisposable
    {
        /// <summary>
        /// structured data to store rules, in order of precidence, as follows:
        ///     first, order by size of the TargetSymbolSeries. Patterns which match more symbols always take precidence over patterns which match less symbols
        /// </summary>
        private IDictionary<int, IList<BasicRule>> rulesByTargetSymbol;

        private NativeOrderedMultiDictionary<BasicRule.Blittable> blittableRulesByTargetSymbol;
        private SystemLevelRuleNativeData nativeRuleData;

        /// <summary>
        /// Stores the maximum number of parameters that could be captured by each symbol's maximum number of possible alternative matches
        /// </summary>
        private IDictionary<int, MaxMatchMemoryRequirements> maxMemoryRequirementsPerSymbol;

        /// <summary>
        /// The number of global runtime parameters
        /// </summary>
        public int GlobalParameters { get; private set; }

        public int branchOpenSymbol;
        public int branchCloseSymbol;
        /// <summary>
        /// Defaults to false. fully ordering agnostic matching is not yet implemented, setting to true will result in an approximation
        ///     with some failures on edge cases involving subsets of matches. look at the context matcher tests for more details.
        /// </summary>
        public bool orderingAgnosticContextMatching = false;

        // currently just used for blocking out context matching. could be used in the future to exclude rule application from specific symbols, too.
        // if that improves runtime.
        public ISet<int> ignoredCharacters;

        public bool isDisposed = false;

        public LSystem(
            IEnumerable<BasicRule> rules,
            SystemLevelRuleNativeData nativeRuleData,
            int expectedGlobalParameters = 0,
            int branchOpenSymbol = '[',
            int branchCloseSymbol = ']',
            ISet<int> ignoredCharacters = null)
        {
            GlobalParameters = expectedGlobalParameters;
            this.nativeRuleData = nativeRuleData;

            this.branchOpenSymbol = branchOpenSymbol;
            this.branchCloseSymbol = branchCloseSymbol;
            this.ignoredCharacters = ignoredCharacters == null ? new HashSet<int>() : ignoredCharacters;

            rulesByTargetSymbol = new Dictionary<int, IList<BasicRule>>();
            foreach (var rule in rules)
            {
                var targetSymbols = rule.TargetSymbol;
                if (!rulesByTargetSymbol.TryGetValue(targetSymbols, out var ruleList))
                {
                    rulesByTargetSymbol[targetSymbols] = ruleList = new List<BasicRule>();
                }
                ruleList.Add(rule);
            }

            maxMemoryRequirementsPerSymbol = new Dictionary<int, MaxMatchMemoryRequirements>();
            foreach (var symbol in rulesByTargetSymbol.Keys.ToList())
            {
                rulesByTargetSymbol[symbol] = rulesByTargetSymbol[symbol]
                    .OrderByDescending(x => 
                        (x.ContextPrefix.IsValid ? x.ContextPrefix.graphNodeMemSpace.length : 0) +
                        (x.ContextSuffix.IsCreated ? x.ContextSuffix.graphNodeMemSpace.length : 0))
                    .ToList();
                var conditionalRules = rulesByTargetSymbol[symbol].Where(x => x.HasConditional).ToArray();
                // can potentially match all conditionals, plus one extra non-conditional
                // TODO: greedy allocation. the count may not be this high.
                var maxPossibleMatches = conditionalRules.Length + 1;

                var maxParamsCapturedByAllConditionals = conditionalRules.Sum(x => x.CapturedLocalParameterCount);
                var maxParamsForNonConditionalIndividual = rulesByTargetSymbol[symbol]
                    .Where(x => !x.HasConditional)
                    .Select(x => x.CapturedLocalParameterCount)
                    .DefaultIfEmpty().Max();
                // Greedy estimate for maximum possible parameter match.
                // TODO: can optimize this, since if the max non-conditional parameters come first, it will never match at the same time as all
                //      the following conditionals
                var maximumPossibleParameterMatch = maxParamsCapturedByAllConditionals + maxParamsForNonConditionalIndividual;
                if (maximumPossibleParameterMatch > ushort.MaxValue)
                {
                    throw new LSystemRuntimeException($"Rules with more than {ushort.MaxValue} captured local parameters over all conditional options");
                }
                maxMemoryRequirementsPerSymbol[symbol] = new MaxMatchMemoryRequirements
                {
                    maxParameters = maximumPossibleParameterMatch,
                    maxPossibleMatches = maxPossibleMatches
                };
            }

            blittableRulesByTargetSymbol = NativeOrderedMultiDictionary<BasicRule.Blittable>.WithMapFunction(
                rulesByTargetSymbol,
                rule => rule.AsBlittable(),
                Allocator.Persistent);
        }
        public LSystemState<float> StepSystem(LSystemState<float> systemState, float[] globalParameters = null, bool disposeOldSystem = true)
        {
            var stepper = StepSystemJob(systemState, globalParameters);
            LSystemState<float> nextState = null;
            while (nextState == null)
            {
                nextState = stepper.StepToNextState();
            }
            if (disposeOldSystem)
            {
                systemState.currentSymbols.Dispose();
            }
            return nextState;
        }

        /// <summary>
        /// Step the given <paramref name="systemState"/>. returning the new system state. No modifications are made the the system sate
        /// Rough system step process:
        ///     1. iterate through the current system state, retrieving the maximum # of parameters that can be captured for each symbol.
        ///         Track symbols with conditionals seperately. during the match phase, every rule which is a conditional will match if possible
        ///         and if no higher-ranking rule has matched yet. Allocate memory for the possible match array, and the parameter match array
        ///     2. batch process each potential match, and each possible conditional. stop processing rules for a specific symbol once any non-conditional
        ///         rule matches. write all matched parameters for every matched rule into temporary parameter memory space.
        ///         TODO: will have to store the "range" of rules which have matched. it is gauranteed to be contiguous. For this reason, delay stochastic selection
        ///         until later, to avoid storing any extra per-rule data
        ///     3. Match selection: For each symbol, iterate through the possible matches identified in #2. When the first match is found, populate info about the selected
        ///         match and the size of the replacement into singleton
        ///     4. Sum up the new symbol string length, and allocate memory for it.
        ///     5. Batch process each symbol in parallel again, this time writing the replacement symbols themselves into memory. Will rely on reading parameters
        ///         out of the memory allocated in
        /// </summary>
        /// <param name="systemState">The entire state of the L-system. no modifications are made to this object or the contained properties.</param>
        /// <param name="globalParameters">The global parameters, if any</param>
        public LSystemSteppingState StepSystemJob(LSystemState<float> systemState, float[] globalParameters = null)
        {
            if (isDisposed)
            {
                throw new LSystemRuntimeException($"LSystem has already been disposed");
            }
            UnityEngine.Profiling.Profiler.BeginSample("L system step");
            if (globalParameters == null)
            {
                globalParameters = new float[0];
            }

            var globalParamSize = globalParameters.Length;
            if (globalParamSize != GlobalParameters)
            {
                throw new LSystemRuntimeException($"Incomplete parameters provided. Expected {GlobalParameters} parameters but got {globalParamSize}");
            }


            // 1.
            UnityEngine.Profiling.Profiler.BeginSample("Paramter counts");
            var matchSingletonData = new NativeArray<LSystemSingleSymbolMatchData>(systemState.currentSymbols.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var parameterTotalSum = 0;
            var possibleMatchesTotalSum = 0;
            for (int i = 0; i < systemState.currentSymbols.Length; i++)
            {
                var symbol = systemState.currentSymbols[i];
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

            var parameterMemory = new NativeArray<float>(parameterTotalSum, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            UnityEngine.Profiling.Profiler.EndSample();

            // 2.
            UnityEngine.Profiling.Profiler.BeginSample("matching");
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            var tmpBranchingCache = new SymbolStringBranchingCache(
                branchOpenSymbol,
                branchCloseSymbol,
                ignoredCharacters,
                nativeRuleData);
            tmpBranchingCache.BuildJumpIndexesFromSymbols(systemState.currentSymbols.symbols);
            UnityEngine.Profiling.Profiler.EndSample();



            var random = systemState.randomProvider;
            var globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);
            var tempState = new LSystemSteppingState
            {
                branchingCache = tmpBranchingCache,
                sourceSymbolString = systemState.currentSymbols,
                rulesByTargetSymbol = rulesByTargetSymbol,

                tmpParameterMemory = parameterMemory,
                operatorDefinitions = nativeRuleData.dynamicOperatorMemory,
                structExpressionSpace = nativeRuleData.structExpressionMemorySpace
            };
            var tempStateHandle = GCHandle.Alloc(tempState);

            var prematchJob = new RuleCompleteMatchJob
            {
                matchSingletonData = matchSingletonData,

                sourceData = systemState.currentSymbols,
                tmpParameterMemory = parameterMemory,

                globalOperatorData = nativeRuleData.dynamicOperatorMemory,
                outcomes = nativeRuleData.ruleOutcomeMemorySpace,
                globalParams = globalParamNative,

                blittableRulesByTargetSymbol = blittableRulesByTargetSymbol,
                branchingCache = tmpBranchingCache,
                seed = random.NextUInt()
            };

            var matchingJobHandle = prematchJob.ScheduleBatch(
                matchSingletonData.Length,
                100);

            tempState.randResult = random;

            UnityEngine.Profiling.Profiler.EndSample();

            // 4.
            UnityEngine.Profiling.Profiler.BeginSample("replacement counting");

            var totalSymbolLength = new NativeArray<int>(1, Allocator.Persistent);
            var totalSymbolParameterCount = new NativeArray<int>(1, Allocator.Persistent);
            tempState.totalSymbolCount = totalSymbolLength;
            tempState.totalSymbolParameterCount = totalSymbolParameterCount;

            var totalSymbolLengthJob = new RuleReplacementSizeJob
            {
                matchSingletonData = matchSingletonData,
                totalResultSymbolCount = totalSymbolLength,
                totalResultParameterCount = totalSymbolParameterCount,
                sourceParameterIndexes = systemState.currentSymbols.newParameters.indexing,
            };
            var totalSymbolLengthDependency = totalSymbolLengthJob.Schedule(matchingJobHandle);

            tempState.preAllocationStep = totalSymbolLengthDependency;

            UnityEngine.Profiling.Profiler.EndSample();

            tempState.matchSingletonData = matchSingletonData;
            tempState.globalParamNative = globalParamNative;
            tempState.tempStateHandle = tempStateHandle;

            UnityEngine.Profiling.Profiler.EndSample();
            return tempState;
        }

        public static Unity.Mathematics.Random RandomFromIndexAndSeed(uint index, uint seed)
        {
            var r = new Unity.Mathematics.Random(index);
            r.NextUInt();
            r.state = r.state ^ seed;
            return r;
        }

        public void Dispose()
        {
            if (isDisposed) return;
            this.nativeRuleData.Dispose();
            this.blittableRulesByTargetSymbol.Dispose();
            isDisposed = true;
        }
    }

    /// <summary>
    /// Class used to track intermediate state during the lsystem step. accessed from multiple job threads
    /// beware of multithreading
    /// </summary>
    public class LSystemSteppingState
    {
        public SymbolStringBranchingCache branchingCache;
        public SymbolString<float> sourceSymbolString;
        public IDictionary<int, IList<BasicRule>> rulesByTargetSymbol;
        public Unity.Mathematics.Random randResult;

        /// <summary>
        /// A job handle too the <see cref="RuleReplacementSizeJob"/> which will count up the total replacement size
        /// </summary>
        public JobHandle preAllocationStep;
        public JobHandle finalDependency;
        private StepState stepState = StepState.MATCHING;


        public NativeArray<int> totalSymbolCount;
        public NativeArray<int> totalSymbolParameterCount;

        private SymbolString<float> target;

        public NativeArray<LSystemSingleSymbolMatchData> matchSingletonData;
        public NativeArray<float> tmpParameterMemory;
        public NativeArray<OperatorDefinition> operatorDefinitions;
        public NativeArray<StructExpression> structExpressionSpace;

        public NativeArray<float> globalParamNative;
        public GCHandle tempStateHandle; // LSystemSteppingState. self


        private enum StepState
        {
            MATCHING,
            REPLACING,
            COMPLETE
        }

        /// <summary>
        /// Completes the currently pending job, and perform setup for the next, if it exists.
        ///     will return the complete state if the last job was completed
        /// </summary>
        /// <returns>null if there is work remaining. the new state object if done</returns>
        public LSystemState<float> StepToNextState()
        {
            switch (stepState)
            {
                case StepState.MATCHING:
                    this.CompleteIntermediateAndPerformAllocations();
                    return null;
                case StepState.REPLACING:
                    return this.CompleteJobAndGetNextState();
                case StepState.COMPLETE:
                default:
                    throw new System.Exception("stepper state is complete. no more steps");
            }
        }
        public void ForceCompletePendingJobsAndDeallocate()
        {
            switch (stepState)
            {
                case StepState.MATCHING:
                    preAllocationStep.Complete();
                    totalSymbolCount.Dispose();
                    totalSymbolParameterCount.Dispose();
                    globalParamNative.Dispose();
                    tmpParameterMemory.Dispose();
                    matchSingletonData.Dispose();
                    break;
                case StepState.REPLACING:
                    finalDependency.Complete();
                    break;
                case StepState.COMPLETE:
                default:
                    return;
            }
            if (branchingCache.IsCreated) branchingCache.Dispose();
            if (this.target.IsCreated) target.Dispose();
            tempStateHandle.Free();
        }


        private void CompleteIntermediateAndPerformAllocations()
        {
            if(stepState != StepState.MATCHING)
            {
                throw new System.Exception("stepper state not compatible");
            }
            preAllocationStep.Complete();
            var totalNewSymbolSize = totalSymbolCount[0];
            var totalNewParamSize = totalSymbolParameterCount[0];
            this.target = new SymbolString<float>(totalNewSymbolSize, totalNewParamSize, Allocator.Persistent);
            totalSymbolCount.Dispose();
            totalSymbolParameterCount.Dispose();

            // 5
            UnityEngine.Profiling.Profiler.BeginSample("generating replacements");

            var replacementJob = new RuleReplacementJob
            {
                tmpSteppingStateHandle = tempStateHandle,
                globalParametersArray = globalParamNative,

                parameterMatchMemory = tmpParameterMemory,
                matchSingletonData = matchSingletonData,
                globalOperatorData = operatorDefinitions,

                sourceData = sourceSymbolString,
                structExpressionSpace = structExpressionSpace,

                targetData = target
            };

            JobHandle replacementDependency = replacementJob.Schedule(
                matchSingletonData.Length,
                100
            );

            UnityEngine.Profiling.Profiler.EndSample();

            var cleanupJob = new LSystemStepCleanupJob
            {
                tmpSteppingStateHandle = tempStateHandle
            };

            finalDependency = cleanupJob.Schedule(replacementDependency);
            stepState = StepState.REPLACING;
        }

        private LSystemState<float> CompleteJobAndGetNextState()
        {
            if (stepState != StepState.REPLACING)
            {
                throw new System.Exception("stepper state not compatible");
            }
            finalDependency.Complete();
            var newResult = new LSystemState<float>
            {
                randomProvider = randResult,
                currentSymbols = this.target
            };
            branchingCache.Dispose();
            stepState = StepState.COMPLETE;
            return newResult;
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
            var rnd = LSystem.RandomFromIndexAndSeed(((uint)startIndex) + 1, seed);
            for (int i = 0; i < batchSize; i++)
            {
                this.ExecuteAtIndex(i + startIndex, forwardsMatchHelperStack, ref rnd);
            }
        }
        private void ExecuteAtIndex(
            int indexInSymbols,
            TmpNativeStack<SymbolStringBranchingCache.BranchEventData> helperStack,
            ref Unity.Mathematics.Random random) {
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

        public NativeArray<JaggedIndexing> sourceParameterIndexes;
        public void Execute()
        {
            var totalResultSymbolSize = 0;
            var totalResultParamSize = 0;
            for (int i = 0; i < matchSingletonData.Length; i++)
            {
                var singleton = matchSingletonData[i];
                singleton.replacementSymbolIndexing.index = totalResultSymbolSize;
                singleton.replacementParameterIndexing.index = totalResultParamSize;
                matchSingletonData[i] = singleton;
                if (singleton.isTrivial)
                {
                    totalResultSymbolSize += 1;
                    totalResultParamSize += sourceParameterIndexes[i].length;
                }
                else
                {
                    totalResultSymbolSize += singleton.replacementSymbolIndexing.length;
                    totalResultParamSize += singleton.replacementParameterIndexing.length;
                }
            }

            totalResultSymbolCount[0] = totalResultSymbolSize;
            totalResultParameterCount[0] = totalResultParamSize;
        }
    }

    public struct RuleReplacementJob : IJobParallelFor
    {
        public GCHandle tmpSteppingStateHandle; // LSystemSteppingState
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
        public NativeArray<OperatorDefinition> globalOperatorData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<StructExpression> structExpressionSpace;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public SymbolString<float> sourceData;

        [NativeDisableParallelForRestriction]
        public SymbolString<float> targetData;

        public void Execute(int indexInSymbols)
        {
            var tmpSteppingState = (LSystemSteppingState)tmpSteppingStateHandle.Target;

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

            var rulesByTargetSymbol = tmpSteppingState.rulesByTargetSymbol;
            if (!rulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList == null || ruleList.Count <= 0)
            {
                matchSingleton.errorCode = LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME;
                matchSingletonData[indexInSymbols] = matchSingleton;
                // could recover gracefully. but for now, going to force a failure
                return;
            }

            var rule = ruleList[matchSingleton.matchedRuleIndexInPossible];

            rule.WriteReplacementSymbols(
                globalParametersArray,
                parameterMatchMemory,
                targetData,
                matchSingleton,
                globalOperatorData,
                structExpressionSpace
                );
        }
    }

    public struct LSystemStepCleanupJob : IJob
    {
        public GCHandle tmpSteppingStateHandle; // LSystemSteppingState
        public void Execute()
        {
            tmpSteppingStateHandle.Free();
        }
    }
}
