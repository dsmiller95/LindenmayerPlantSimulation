using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using LSystem.Runtime.SystemRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

    public class LSystemState<T>
    {
        public SymbolString<T> currentSymbols { get; set; }
        public Unity.Mathematics.Random randomProvider;
    }

    public class DefaultLSystemState : LSystemState<float>
    {
        public DefaultLSystemState(string axiom, int seed): this(axiom, (uint)seed)
        {}
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
                if(maxParamsAtSymbool > byte.MaxValue)
                {
                    throw new LSystemRuntimeException($"Rules with more than {byte.MaxValue} captured local parameters are not supported");
                }
                maxParameterCapturePerSymbol[symbol] = maxParamsAtSymbool;
            }
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
        public LSystemState<float> StepSystem(LSystemState<float> systemState, float[] globalParameters = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L system step");
            if(globalParameters == null)
            {
                globalParameters = new float[0];
            }

            var globalParamSize = globalParameters.Length;
            if (globalParamSize != GlobalParameters)
            {
                throw new LSystemRuntimeException($"Incomplete parameters provided. Expected {GlobalParameters} parameters but got {globalParamSize}");
            }


            // 1.
            var matchSingletonData = new NativeArray<LSystemStepMatchIntermediate>(systemState.currentSymbols.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var parameterTotalSum = 0;
            for (int i = 0; i < systemState.currentSymbols.Length; i++)
            {
                var symbol = systemState.currentSymbols[i];
                var matchData = new LSystemStepMatchIntermediate()
                {
                    parametersStartIndex = parameterTotalSum
                };
                if(this.maxParameterCapturePerSymbol.TryGetValue(symbol, out var maxParameterCount))
                {
                    parameterTotalSum += this.maxParameterCapturePerSymbol[symbol];
                    matchData.isTrivial = false;
                }else
                {
                    matchData.isTrivial = true;
                    matchData.matchedParametersCount = 0;
                }
                matchSingletonData[i] = matchData;
            }

            // 2.
            var parameterMemory = new NativeArray<float>(parameterTotalSum, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // 3.
            var tmpBranchingCache = new SymbolStringBranchingCache(branchOpenSymbol, branchCloseSymbol, this.ignoredCharacters);
            tmpBranchingCache.SetTargetSymbolString(systemState.currentSymbols);

            var random = systemState.randomProvider;
            var randGens = new NativeArray<Unity.Mathematics.Random>(Environment.ProcessorCount, Allocator.TempJob);
            for (int i = 0; i < randGens.Length; i++)
            {
                randGens[i] = new Unity.Mathematics.Random((uint)random.NextInt());
            }

            var matchingJob = new RuleMatchJob
            {
                globalParameters = globalParameters,
                matchSingletonData = matchSingletonData,
                parameterMatchMemory = parameterMemory,
                rulesByTargetSymbol = this.rulesByTargetSymbol,
                sourceSymbolString = systemState.currentSymbols,
                branchingCache = tmpBranchingCache,

                RandomGenerator = randGens
            };


            //var matchJobDependency = matchingJob.Schedule(
            //    matchSingletonData.Length,
            //    100
            //);
            //matchingJob.Run(matchSingletonData.Length);

            for (int i = 0; i < matchSingletonData.Length; i++)
            {
                matchingJob.Execute(i);
            }
            randGens.Dispose();

            var nextSymbolLength = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var totalSymbolLengthJob = new RuleReplacementSizeJob
            {
                matchSingletonData = matchSingletonData,
                replacementSymbolSizeOutput = nextSymbolLength
            };
            //totalSymbolLengthJob.Run();
            var totalSymbolLengthDependency = totalSymbolLengthJob.Schedule();
            totalSymbolLengthDependency.Complete();

            var nextSymbolSize = nextSymbolLength[0];
            nextSymbolLength.Dispose();


            var nextSymbols = new int[nextSymbolSize];
            var nextParameters = new float[nextSymbolSize][];
            var generatedSymbols = new SymbolString<float>(nextSymbols, nextParameters);

            var replacementJob = new RuleReplacementJob
            {
                matchSingletonData = matchSingletonData,
                parameterMatchMemory = parameterMemory,

                globalParameters = globalParameters,
                rulesByTargetSymbol = rulesByTargetSymbol,
                sourceSymbolString = systemState.currentSymbols,
                targetSymbolString = generatedSymbols
            };

            //var replacementDependency = replacementJob.Schedule(
            //    matchSingletonData.Length,
            //    100,
            //    totalSymbolLengthDependency
            //);
            //replacementDependency.Complete();
            //replacementJob.Run(matchSingletonData.Length);
            for (int i = 0; i < matchSingletonData.Length; i++)
            {
                replacementJob.Execute(i);
            }

            matchSingletonData.Dispose();
            parameterMemory.Dispose();

            var realNextState = new LSystemState<float>()
            {
                randomProvider = random,
                currentSymbols = generatedSymbols
            };

            //var nextState = new LSystemState<float>()
            //{
            //    randomProvider = systemState.randomProvider
            //};
            //var resultString = GenerateNextSymbols(systemState.currentSymbols, ref nextState.randomProvider, globalParameters).ToList();
            //nextState.currentSymbols = SymbolString<float>.ConcatAll(resultString);
            UnityEngine.Profiling.Profiler.EndSample();
            return realNextState;
        }

        private SymbolString<float>[] GenerateNextSymbols(SymbolString<float> symbolState, ref Unity.Mathematics.Random random, float[] globalParameters)
        {
            var tmpBranchingCache = new SymbolStringBranchingCache(branchOpenSymbol, branchCloseSymbol, this.ignoredCharacters);
            tmpBranchingCache.SetTargetSymbolString(symbolState);

            var resultArray = new SymbolString<float>[symbolState.symbols.Length];
            for (int symbolIndex = 0; symbolIndex < symbolState.symbols.Length; symbolIndex++)
            {
                var symbol = symbolState.symbols[symbolIndex];
                var parameters = symbolState.parameters[symbolIndex];
                var ruleApplied = false;
                if (rulesByTargetSymbol.TryGetValue(symbol, out var ruleList) && ruleList != null && ruleList.Count > 0)
                {
                    foreach (var rule in ruleList)
                    {
                        // check if match
                        var result = rule.ApplyRule(
                            tmpBranchingCache,
                            symbolState,
                            symbolIndex,
                            ref random,
                            globalParameters);// todo
                        if (result != null)
                        {
                            resultArray[symbolIndex] = result;
                            ruleApplied = true;
                            break;
                        }
                    }
                }
                if (!ruleApplied)
                {
                    // if none of the rules match, which could happen if all of the matches for this char require additional subsequent characters
                    // or if there are no rules
                    resultArray[symbolIndex] = new SymbolString<float>(symbol, parameters);
                }
            }
            return resultArray;
        }
    }

    public struct RuleMatchJob : IJobParallelFor
    {
        public float[] globalParameters;

        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;
        public NativeArray<float> parameterMatchMemory;
        public IDictionary<int, IList<IRule<float>>> rulesByTargetSymbol;
        public SymbolString<float> sourceSymbolString;
        // TODO: make the branching cache thread-safe, by making it Read-only. can iterate through the symbols once before 
        //  opening the job.
        public SymbolStringBranchingCache branchingCache;


        [NativeSetThreadIndex]
#pragma warning disable IDE0044 // Add readonly modifier
        private int threadIndex;
#pragma warning restore IDE0044 // Add readonly modifier

        [NativeDisableParallelForRestriction]
        [DeallocateOnJobCompletion]
        public NativeArray<Unity.Mathematics.Random> RandomGenerator;


        public void Execute(int indexInSymbols)
        {
            var rnd = RandomGenerator[threadIndex];

            var matchSingleton = matchSingletonData[indexInSymbols];
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, then no parameters are captured
                //  and no transformation will take place.
                return;
            }
            var symbol = sourceSymbolString.symbols[indexInSymbols];

            if (!rulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList == null || ruleList.Count <= 0)
            {
                matchSingleton.errorCode = LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_MATCH_TIME;
                return;
                // could recover gracefully. but for now, going to force a failure
                //matchSingleton.isTrivial = true;
                //matchSingleton.matchedParametersCount = 0;
                //matchSingletonData[indexInSymbols] = matchSingleton;
                //return;
            }
            var ruleMatched = false;
            for (byte i = 0; i < ruleList.Count; i++)
            {
                var rule = ruleList[i];
                var capturedParameters = rule.PreMatchCapturedParameters(
                    branchingCache,
                    sourceSymbolString,
                    indexInSymbols,
                    globalParameters,
                    ref rnd,
                    out var replacementLength,
                    out var selectedReplacement);
                if (capturedParameters != null)
                {
                    for (int paramIndex = 0; paramIndex < capturedParameters.Length; paramIndex++)
                    {
                        parameterMatchMemory[matchSingleton.parametersStartIndex + paramIndex] = capturedParameters[paramIndex];
                    }
                    if (capturedParameters.Length > byte.MaxValue)
                    {
                        matchSingleton.errorCode = LSystemMatchErrorCode.TOO_MANY_PARAMETERS;
                        return;
                    }
                    matchSingleton.matchedParametersCount = (byte)capturedParameters.Length;
                    matchSingleton.matchedRuleIndexInPossible = i;
                    matchSingleton.selectedReplacementPattern = selectedReplacement;
                    matchSingleton.replacementSymbolLength = replacementLength;

                    ruleMatched = true;
                    break;
                }
            }
            if(ruleMatched == false)
            {
                matchSingleton.matchedParametersCount = 0;
                matchSingleton.replacementSymbolLength = 1;
                matchSingleton.isTrivial = true;
            }
            matchSingletonData[indexInSymbols] = matchSingleton;
            RandomGenerator[threadIndex] = rnd;
        }
    }
    public struct RuleReplacementSizeJob : IJob
    {
        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;
        public NativeArray<int> replacementSymbolSizeOutput;
        public void Execute()
        {
            var totalSymbolSize = 0;
            for (int i = 0; i < matchSingletonData.Length; i++)
            {
                var singleton = matchSingletonData[i];
                singleton.replacementSymbolStartIndex = totalSymbolSize;
                matchSingletonData[i] = singleton;
                if (singleton.isTrivial)
                {
                    totalSymbolSize += 1;
                }else
                {
                    totalSymbolSize += singleton.replacementSymbolLength;
                }
            }
            replacementSymbolSizeOutput[0] = totalSymbolSize;
        }
    }

    public struct RuleReplacementJob : IJobParallelFor
    {
        public float[] globalParameters;

        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;
        [ReadOnly]
        public NativeArray<float> parameterMatchMemory;
        public IDictionary<int, IList<IRule<float>>> rulesByTargetSymbol;
        public SymbolString<float> sourceSymbolString;
        public SymbolString<float> targetSymbolString;

        public void Execute(int indexInSymbols)
        {
            var matchSingleton = matchSingletonData[indexInSymbols];
            var symbol = sourceSymbolString.symbols[indexInSymbols];
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, just copy the symbol over, nothing else.
                var targetIndex = matchSingleton.replacementSymbolStartIndex;
                targetSymbolString.symbols[targetIndex] = symbol;
                targetSymbolString.parameters[targetIndex] = sourceSymbolString.parameters[indexInSymbols];
                return;
            }

            if (!rulesByTargetSymbol.TryGetValue(symbol, out var ruleList) || ruleList == null || ruleList.Count <= 0)
            {
                matchSingleton.errorCode = LSystemMatchErrorCode.TRIVIAL_SYMBOL_NOT_INDICATED_AT_REPLACEMENT_TIME;
                matchSingletonData[indexInSymbols] = matchSingleton;
                // could recover gracefully. but for now, going to force a failure
                return;
            }

            var rule = ruleList[matchSingleton.matchedRuleIndexInPossible];

            rule.WriteReplacementSymbols(
                globalParameters,
                matchSingleton.selectedReplacementPattern,
                parameterMatchMemory,
                matchSingleton.parametersStartIndex,
                matchSingleton.matchedParametersCount,
                targetSymbolString,
                matchSingleton.replacementSymbolStartIndex,
                matchSingleton.replacementSymbolLength
                );
        }
    }
}
