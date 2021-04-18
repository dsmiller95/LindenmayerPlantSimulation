using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using LSystem.Runtime.SystemRuntime;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

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
            return new LSystem(
                RuleParser.CompileRules(
                        rules,
                        globalParameters
                        ),
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
            currentSymbols = new SymbolString<float>(axiom);
            randomProvider = new Unity.Mathematics.Random(seed);
        }
    }

    public class LSystem
    {
        /// <summary>
        /// structured data to store rules, in order of precidence, as follows:
        ///     first, order by size of the TargetSymbolSeries. Patterns which match more symbols always take precidence over patterns which match less symbols
        /// </summary>
        private IDictionary<int, IList<IRule<float>>> rulesByTargetSymbol;
        /// <summary>
        /// Stores the maximum number of parameters that can be captured by each symbol
        /// </summary>
        private IDictionary<int, int> maxParameterCapturePerSymbol;

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

        public LSystem(
            IEnumerable<IRule<float>> rules,
            int expectedGlobalParameters = 0,
            int branchOpenSymbol = '[',
            int branchCloseSymbol = ']',
            ISet<int> ignoredCharacters = null)
        {
            GlobalParameters = expectedGlobalParameters;
            this.branchOpenSymbol = branchOpenSymbol;
            this.branchCloseSymbol = branchCloseSymbol;
            this.ignoredCharacters = ignoredCharacters == null ? new HashSet<int>() : ignoredCharacters;

            rulesByTargetSymbol = new Dictionary<int, IList<IRule<float>>>();
            maxParameterCapturePerSymbol = new Dictionary<int, int>();
            foreach (var rule in rules)
            {
                var targetSymbols = rule.TargetSymbol;
                if (!rulesByTargetSymbol.TryGetValue(targetSymbols, out var ruleList))
                {
                    rulesByTargetSymbol[targetSymbols] = ruleList = new List<IRule<float>>();
                }
                ruleList.Add(rule);
            }
            foreach (var symbol in rulesByTargetSymbol.Keys.ToList())
            {
                rulesByTargetSymbol[symbol] = rulesByTargetSymbol[symbol]
                    .OrderByDescending(x => (x.ContextPrefix?.targetSymbolSeries?.Length ?? 0) + (x.ContextSuffix?.targetSymbolSeries?.Length ?? 0))
                    .ToList();
                var maxParamsAtSymbool = rulesByTargetSymbol[symbol].Max(x => x.CapturedLocalParameterCount);
                if (maxParamsAtSymbool > byte.MaxValue)
                {
                    throw new LSystemRuntimeException($"Rules with more than {byte.MaxValue} captured local parameters are not supported");
                }
                maxParameterCapturePerSymbol[symbol] = maxParamsAtSymbool;
            }
        }
        public LSystemState<float> StepSystem(LSystemState<float> systemState, float[] globalParameters = null, bool disposeOldSystem = true)
        {
            var stepper = StepSystemJob(systemState, globalParameters);
            stepper.CompleteIntermediateAndPerformAllocations();
            var result = stepper.CompleteJobAndGetNextState();

            if (disposeOldSystem)
            {
                systemState.currentSymbols.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Step the given <paramref name="systemState"/>. returning the new system state. No modifications are made the the system sate
        /// Rough system step process:
        ///     1. iterate through the current system state, retrieving the maximum # of parameters needed for each symbol.
        ///     2. allocate memory to store any parameters captured during the symbol matching process, based on the sum of the counts from #1
        ///     3. batch process the symbols in parallel, each symbol writing an identifier of the specific rule which was matched and all the captured local parameters
        ///         into pre-allocated memory. Also write the size of the replacement symbols to memory. Stochastic rule selection does happen in this step,
        ///         since stochastic rules in a set may have different replacement symbol sizes
        ///     4. Sum up the new symbol string length, and allocate memory for it.
        ///     5. Batch process each symbol in parallel again, this time writing the replacement symbols themselves into memory. Will rely on reading parameters
        ///         out of the memory allocated in
        /// </summary>
        /// <param name="systemState">The entire state of the L-system. no modifications are made to this object or the contained properties.</param>
        /// <param name="globalParameters">The global parameters, if any</param>
        public LSystemSteppingState StepSystemJob(LSystemState<float> systemState, float[] globalParameters = null)
        {
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
            var matchSingletonData = new NativeArray<LSystemStepMatchIntermediate>(systemState.currentSymbols.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var parameterTotalSum = 0;
            for (int i = 0; i < systemState.currentSymbols.Length; i++)
            {
                var symbol = systemState.currentSymbols[i];
                var matchData = new LSystemStepMatchIntermediate()
                {
                    parametersStartIndex = parameterTotalSum
                };
                if (maxParameterCapturePerSymbol.TryGetValue(symbol, out var maxParameterCount))
                {
                    parameterTotalSum += maxParameterCount;
                    matchData.isTrivial = false;
                }
                else
                {
                    matchData.isTrivial = true;
                    matchData.matchedParametersCount = 0;
                }
                matchSingletonData[i] = matchData;
            }

            // 2.
            var parameterMemory = new NativeArray<float>(parameterTotalSum, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            UnityEngine.Profiling.Profiler.EndSample();


            // 3.
            UnityEngine.Profiling.Profiler.BeginSample("matching");
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            var tmpBranchingCache = new SymbolStringBranchingCache(branchOpenSymbol, branchCloseSymbol, ignoredCharacters);
            tmpBranchingCache.BuildJumpIndexesFromSymbols(systemState.currentSymbols.symbols);
            UnityEngine.Profiling.Profiler.EndSample();

            var random = systemState.randomProvider;


            var tempState = new LSystemSteppingState
            {
                branchingCache = tmpBranchingCache,
                sourceSymbolString = systemState.currentSymbols,
                rulesByTargetSymbol = rulesByTargetSymbol
            };
            var tempStateHandle = GCHandle.Alloc(tempState);

            var globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);

            var matchingJob = new RuleMatchJob
            {
                globalParametersArray = globalParamNative,
                matchSingletonData = matchSingletonData,
                parameterMatchMemory = parameterMemory,

                tmpSteppingStateHandle = tempStateHandle,

                sourceSymbols = systemState.currentSymbols.symbols,
                sourceParameterIndexes = systemState.currentSymbols.parameterIndexes,
                sourceParameters = systemState.currentSymbols.parameters,

                seed = random.NextUInt()
            };

            tempState.randResult = random;

            var matchJobHandle = matchingJob.Schedule(
                matchSingletonData.Length,
                100
            );
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
                sourceParameterIndexes = systemState.currentSymbols.parameterIndexes,
            };
            //totalSymbolLengthJob.Run();
            var totalSymbolLengthDependency = totalSymbolLengthJob.Schedule(matchJobHandle);

            tempState.preAllocationStep = totalSymbolLengthDependency;

            UnityEngine.Profiling.Profiler.EndSample();

            tempState.matchSingletonData = matchSingletonData;
            tempState.parameterMemory = parameterMemory;
            tempState.globalParamNative = globalParamNative;
            tempState.tempStateHandle = tempStateHandle;

            // 5
            //UnityEngine.Profiling.Profiler.BeginSample("generating replacements");


            //var replacementJob = new RuleReplacementJob
            //{
            //    matchSingletonData = matchSingletonData,
            //    parameterMatchMemory = parameterMemory,

            //    globalParametersArray = globalParamNative,

            //    tmpSteppingStateHandle = tempStateHandle
            //};

            //var parallelRun = true;
            //JobHandle replacementDependency = default;
            //if (parallelRun)
            //{
            //    replacementDependency = replacementJob.Schedule(
            //        matchSingletonData.Length,
            //        100,
            //        totalSymbolLengthDependency
            //    );
            //}
            //else
            //{
            //    totalSymbolLengthDependency.Complete();
            //    for (int i = 0; i < matchSingletonData.Length; i++)
            //    {
            //        replacementJob.Execute(i);
            //    }
            //    globalParamNative.Dispose();
            //    matchSingletonData.Dispose();
            //    parameterMemory.Dispose();
            //}
            ////replacementJob.Run(matchSingletonData.Length);
            //UnityEngine.Profiling.Profiler.EndSample();

            //var cleanupJob = new LSystemStepCleanupJob
            //{
            //    tmpSteppingStateHandle = tempStateHandle
            //};

            //var dependency = cleanupJob.Schedule(replacementDependency);

            //tempState.finalDependency = dependency;

            //var nextState = new LSystemState<float>()
            //{
            //    randomProvider = systemState.randomProvider
            //};
            //var resultString = GenerateNextSymbols(systemState.currentSymbols, ref nextState.randomProvider, globalParameters).ToList();
            //nextState.currentSymbols = SymbolString<float>.ConcatAll(resultString);
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
    }

    /// <summary>
    /// Class used to track intermediate state during the lsystem step. accessed from multiple job threads
    /// beware of multithreading
    /// </summary>
    public class LSystemSteppingState
    {
        public SymbolStringBranchingCache branchingCache;
        public SymbolString<float> sourceSymbolString;
        public IDictionary<int, IList<IRule<float>>> rulesByTargetSymbol;
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

        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;
        public NativeArray<float> parameterMemory;
        public NativeArray<float> globalParamNative;
        public GCHandle tempStateHandle; // LSystemSteppingState. self


        private enum StepState
        {
            MATCHING,
            REPLACING,
            COMPLETE
        }

        public void CompleteIntermediateAndPerformAllocations()
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

                parameterMatchMemory = parameterMemory,
                matchSingletonData = matchSingletonData,


                sourceSymbols = sourceSymbolString.symbols,
                sourceParameterIndexes = sourceSymbolString.parameterIndexes,
                sourceParameters = sourceSymbolString.parameters,

                targetSymbols = target.symbols,
                targetParameterIndexes = target.parameterIndexes,
                targetParameters = target.parameters
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

        public LSystemState<float> CompleteJobAndGetNextState()
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
            stepState = StepState.COMPLETE;
            return newResult;
        }
    }

    public struct RuleMatchJob : IJobParallelFor
    {
        public GCHandle tmpSteppingStateHandle; // LSystemSteppingState

        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;

        [NativeDisableParallelForRestriction]
        public NativeArray<float> parameterMatchMemory;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public NativeArray<float> globalParametersArray;


        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> sourceSymbols;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<SymbolString<float>.JaggedIndexing> sourceParameterIndexes;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> sourceParameters;


        public uint seed;

        public void Execute(int indexInSymbols)
        {
            var tmpSteppingState = (LSystemSteppingState)tmpSteppingStateHandle.Target;

            var matchSingleton = matchSingletonData[indexInSymbols];
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, then no parameters are captured. the rest of the algo will read directly from the source index
                //  and no transformation will take place.
                return;
            }
            var rulesByTargetSymbol = tmpSteppingState.rulesByTargetSymbol;
            var symbol = sourceSymbols[indexInSymbols];

            if (!rulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList == null || ruleList.Count <= 0)
            {
                matchSingleton.errorCode = LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_MATCH_TIME;
                matchSingletonData[indexInSymbols] = matchSingleton;
                return;
                // could recover gracefully. but for now, going to force a failure
                //matchSingleton.isTrivial = true;
                //matchSingleton.matchedParametersCount = 0;
                //matchSingletonData[indexInSymbols] = matchSingleton;
                //return;
            }
            var branchingCache = tmpSteppingState.branchingCache;
            var rnd = LSystem.RandomFromIndexAndSeed(((uint)indexInSymbols) + 1, seed);
            var globalParameters = globalParametersArray.ToArray();
            var ruleMatched = false;
            for (byte i = 0; i < ruleList.Count; i++)
            {
                var rule = ruleList[i];
                var success = rule.PreMatchCapturedParameters(
                    branchingCache,
                    sourceSymbols,
                    sourceParameterIndexes,
                    sourceParameters,
                    indexInSymbols,
                    globalParameters,
                    parameterMatchMemory,
                    ref rnd,
                    ref matchSingleton);
                if (success)
                {
                    matchSingleton.matchedRuleIndexInPossible = i;
                    ruleMatched = true;
                    break;
                }
            }
            if (ruleMatched == false)
            {
                matchSingleton.isTrivial = true;
            }
            matchSingletonData[indexInSymbols] = matchSingleton;
        }
    }

    [BurstCompile]
    public struct RuleReplacementSizeJob : IJob
    {
        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;
        public NativeArray<int> totalResultSymbolCount;
        public NativeArray<int> totalResultParameterCount;

        public NativeArray<SymbolString<float>.JaggedIndexing> sourceParameterIndexes;
        public void Execute()
        {
            var totalResultSymbolSize = 0;
            var totalResultParamSize = 0;
            for (int i = 0; i < matchSingletonData.Length; i++)
            {
                var singleton = matchSingletonData[i];
                singleton.replacementSymbolStartIndex = totalResultSymbolSize;
                singleton.replacementParameterStartIndex = totalResultParamSize;
                matchSingletonData[i] = singleton;
                if (singleton.isTrivial)
                {
                    totalResultSymbolSize += 1;
                    totalResultParamSize += sourceParameterIndexes[i].length;
                }
                else
                {
                    totalResultSymbolSize += singleton.replacementSymbolLength;
                    totalResultParamSize += singleton.replacementParameterCount;
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
        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;


        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> sourceSymbols;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<SymbolString<float>.JaggedIndexing> sourceParameterIndexes;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> sourceParameters;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> targetSymbols;
        [NativeDisableParallelForRestriction]
        public NativeArray<SymbolString<float>.JaggedIndexing> targetParameterIndexes;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> targetParameters;

        public void Execute(int indexInSymbols)
        {
            var tmpSteppingState = (LSystemSteppingState)tmpSteppingStateHandle.Target;

            var matchSingleton = matchSingletonData[indexInSymbols];
            var symbol = sourceSymbols[indexInSymbols];
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, just copy the symbol over, nothing else.
                var targetIndex = matchSingleton.replacementSymbolStartIndex;
                targetSymbols[targetIndex] = symbol;
                var sourceParamIndexer = sourceParameterIndexes[indexInSymbols];
                targetParameterIndexes[targetIndex] = new SymbolString<float>.JaggedIndexing
                {
                    index = matchSingleton.replacementParameterStartIndex,
                    length = sourceParamIndexer.length
                };
                // when trivial, copy out of the source param array directly. As opposed to reading parameters out oof the parameterMatchMemory when evaluating
                //      a non-trivial match
                for (int i = 0; i < sourceParamIndexer.length; i++)
                {
                    var sourceIndex = sourceParamIndexer.index + i;
                    var targetParamIndex = matchSingleton.replacementParameterStartIndex + i;
                    targetParameters[targetParamIndex] = sourceParameters[sourceIndex];
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

            var globalParameters = globalParametersArray.ToArray();

            rule.WriteReplacementSymbols(
                globalParameters,
                parameterMatchMemory,
                targetSymbols,
                targetParameterIndexes,
                targetParameters,
                matchSingleton
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
