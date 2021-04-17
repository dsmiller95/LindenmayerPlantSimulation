using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using LSystem.Runtime.SystemRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

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
            UnityEngine.Profiling.Profiler.EndSample();


            // 3.
            UnityEngine.Profiling.Profiler.BeginSample("matching");
            UnityEngine.Profiling.Profiler.BeginSample("branch cache");
            var tmpBranchingCache = new SymbolStringBranchingCache(branchOpenSymbol, branchCloseSymbol, this.ignoredCharacters);
            tmpBranchingCache.SetTargetSymbolString(systemState.currentSymbols);
            UnityEngine.Profiling.Profiler.EndSample();

            var random = systemState.randomProvider;

            var rulesByTargetSymbolHandle = GCHandle.Alloc(rulesByTargetSymbol);
            var sourceSymbolStringHandle = GCHandle.Alloc(systemState.currentSymbols);
            var branchingCacheHandle = GCHandle.Alloc(tmpBranchingCache);
            var globalParamNative = new NativeArray<float>(globalParameters, Allocator.Persistent);

            var matchingJob = new RuleMatchJob
            {
                globalParametersArray = globalParamNative,
                matchSingletonData = matchSingletonData,
                parameterMatchMemory = parameterMemory,
                rulesByTargetSymbolHandle = rulesByTargetSymbolHandle,
                sourceSymbolStringHandle = sourceSymbolStringHandle,
                branchingCacheHandle = branchingCacheHandle,

                seed = random.NextUInt()
            };

            var parallelRun = true;

            JobHandle matchJobHandle = default;
            if (parallelRun)
            {
                //matchingJob.Run(matchSingletonData.Length);
                matchJobHandle = matchingJob.Schedule(
                    matchSingletonData.Length,
                    100
                );
            }
            else
            {
                for (int i = 0; i < matchSingletonData.Length; i++)
                {
                    matchingJob.Execute(i);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // 4.
            UnityEngine.Profiling.Profiler.BeginSample("replacement counting");
            var nextSymbolLength = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var totalSymbolLengthJob = new RuleReplacementSizeJob
            {
                matchSingletonData = matchSingletonData,
                replacementSymbolSizeOutput = nextSymbolLength
            };
            //totalSymbolLengthJob.Run();
            var totalSymbolLengthDependency = totalSymbolLengthJob.Schedule(matchJobHandle);
            totalSymbolLengthDependency.Complete();

            var nextSymbolSize = nextSymbolLength[0];
            nextSymbolLength.Dispose();
            UnityEngine.Profiling.Profiler.EndSample();

            // 5
            UnityEngine.Profiling.Profiler.BeginSample("generating replacements");
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
            UnityEngine.Profiling.Profiler.EndSample();

            branchingCacheHandle.Free();
            rulesByTargetSymbolHandle.Free();
            sourceSymbolStringHandle.Free();
            globalParamNative.Dispose();
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

        public static Unity.Mathematics.Random RandomFromIndexAndSeed(uint index, uint seed)
        {
            var r = new Unity.Mathematics.Random(index);
            r.NextUInt();
            r.state = r.state ^ seed;
            return r;
        }
    }

    public struct RuleMatchJob : IJobParallelFor
    {
        public GCHandle rulesByTargetSymbolHandle; // IDictionary<int, IList<IRule<float>>>
        public GCHandle sourceSymbolStringHandle; // SymbolString<float> 
        public GCHandle branchingCacheHandle; // SymbolStringBranchingCache

        public NativeArray<LSystemStepMatchIntermediate> matchSingletonData;

        [NativeDisableParallelForRestriction]
        public NativeArray<float> parameterMatchMemory;
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public NativeArray<float> globalParametersArray;

        public uint seed;


        public void Execute(int indexInSymbols)
        {
            var matchSingleton = matchSingletonData[indexInSymbols];
            if (matchSingleton.isTrivial)
            {
                // if match is trivial, then no parameters are captured
                //  and no transformation will take place.
                return;
            }
            var rulesByTargetSymbol = (IDictionary<int, IList<IRule<float>>>)rulesByTargetSymbolHandle.Target;
            var sourceSymbolString = (SymbolString<float>)sourceSymbolStringHandle.Target;
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
            var branchingCache = (SymbolStringBranchingCache)branchingCacheHandle.Target;
            var rnd = LSystem.RandomFromIndexAndSeed(((uint)indexInSymbols) + 1, seed);
            var globalParameters = globalParametersArray.ToArray();
            var ruleMatched = false;
            for (byte i = 0; i < ruleList.Count; i++)
            {
                var rule = ruleList[i];
                var success = rule.PreMatchCapturedParameters(
                    branchingCache,
                    sourceSymbolString,
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
            if(ruleMatched == false)
            {
                matchSingleton.matchedParametersCount = 0;
                matchSingleton.replacementSymbolLength = 1;
                matchSingleton.isTrivial = true;
            }
            matchSingletonData[indexInSymbols] = matchSingleton;
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
