using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.LSystemEvaluator
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
        [System.Obsolete("should use linker to compile systems")]
        public static LSystemStepper FloatSystem(
           IEnumerable<string> rules,
           string[] globalParameters = null,
           string includedCharacters = "[]ABCDEFD",
           int branchOpenSymbol = '[',
           int branchCloseSymbol = ']')
        {
            var compiledRules = RuleParser.CompileRules(
                        rules,
                        out var nativeRuleData,
                        branchOpenSymbol, branchCloseSymbol,
                        globalParameters
                        );

            var customSymbols = new CustomRuleSymbols
            {
                branchOpenSymbol = branchOpenSymbol,
                branchCloseSymbol = branchCloseSymbol
            };

            return new LSystemStepper(
                compiledRules,
                nativeRuleData,
                customSymbols,
                globalParameters?.Length ?? 0,
                includedContextualCharactersByRuleIndex: new[] { new HashSet<int>(includedCharacters.Select(x => (int)x)) }
            );
        }
    }

    [System.Serializable]
    public class LSystemState<T> : ISerializable where T : unmanaged
    {
        public DependencyTracker<SymbolString<T>> currentSymbols;
        public Unity.Mathematics.Random randomProvider;
        public uint firstUniqueOrganId;
        public uint maxUniqueOrganIds;
        public uint uniquePlantId;
        public bool hasImmatureSymbols = true; // default to being immature, if not set otherwise

        public LSystemState()
        {
        }

        #region Serialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("currentSymbols", currentSymbols);

            info.AddValue("randomSeed", randomProvider.state);

            info.AddValue("firstUniqueOrganId", firstUniqueOrganId);
            info.AddValue("maxUniqueOrganIds", maxUniqueOrganIds);
            info.AddValue("hasImmatureSymbols", hasImmatureSymbols);
        }

        // The special constructor is used to deserialize values.
        private LSystemState(SerializationInfo info, StreamingContext context)
        {
            currentSymbols = info.GetValue<DependencyTracker<SymbolString<T>>>("currentSymbols");
            randomProvider = new Unity.Mathematics.Random(info.GetUInt32("randomSeed"));

            firstUniqueOrganId = info.GetUInt32("firstUniqueOrganId");
            maxUniqueOrganIds = info.GetUInt32("maxUniqueOrganIds");
            hasImmatureSymbols = info.GetBoolean("hasImmatureSymbols");
        }
        #endregion
    }

    public class DefaultLSystemState : LSystemState<float>
    {
        public DefaultLSystemState(string axiom, int seed) : this(axiom, (uint)seed)
        { }
        public DefaultLSystemState(SymbolString<float> axiom, uint seed)
        {
            currentSymbols = new DependencyTracker<SymbolString<float>>(axiom);
            randomProvider = new Unity.Mathematics.Random(seed);
        }
        public DefaultLSystemState(string axiom, uint seed = 1)
        {
            currentSymbols = new DependencyTracker<SymbolString<float>>(SymbolString<float>.FromString(axiom));
            randomProvider = new Unity.Mathematics.Random(seed);
        }
    }

    public struct MaxMatchMemoryRequirements
    {
        public int maxParameters;
    }

    public class LSystemStepper : System.IDisposable
    {
        /// <summary>
        /// structured data to store rules, in order of precidence, as follows:
        ///     first, order by size of the TargetSymbolSeries. Patterns which match more symbols always take precidence over patterns which match less symbols
        /// </summary>
        private IDictionary<int, IList<BasicRule>> rulesByTargetSymbol;

        private DependencyTracker<SystemLevelRuleNativeData> nativeRuleData;

        /// <summary>
        /// The number of global runtime parameters
        /// </summary>
        public int GlobalParameters { get; private set; }

        public CustomRuleSymbols customSymbols;
        /// <summary>
        /// Defaults to false. fully ordering agnostic matching is not yet implemented, setting to true will result in an approximation
        ///     with some failures on edge cases involving subsets of matches. look at the context matcher tests for more details.
        /// </summary>
        public bool orderingAgnosticContextMatching = false;

        public ISet<int>[] includedCharacters;

        public bool isDisposed => nativeRuleData.IsDisposed;

        public LSystemStepper(
            IEnumerable<BasicRule> rules,
            SystemLevelRuleNativeData nativeRuleData,
            CustomRuleSymbols customSymbols,
            int expectedGlobalParameters = 0,
            ISet<int>[] includedContextualCharactersByRuleIndex = null)
        {
            GlobalParameters = expectedGlobalParameters;

            this.customSymbols = customSymbols;

            includedCharacters = includedContextualCharactersByRuleIndex == null ? new HashSet<int>[0] : includedContextualCharactersByRuleIndex;

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

            var maxMemoryRequirementsPerSymbol = new NativeHashMap<int, MaxMatchMemoryRequirements>(rulesByTargetSymbol.Keys.Count(), Allocator.Persistent);
            foreach (var symbol in rulesByTargetSymbol.Keys.ToList())
            {
                rulesByTargetSymbol[symbol] = rulesByTargetSymbol[symbol]
                    .OrderByDescending(x =>
                        (x.ContextPrefix.IsValid ? x.ContextPrefix.graphNodeMemSpace.length : 0) +
                        (x.ContextSuffix.IsCreated ? x.ContextSuffix.graphNodeMemSpace.length : 0))
                    .ToList();

                var maxParamCapturedByAnyRule = rulesByTargetSymbol[symbol]
                    .Select(x => x.CapturedLocalParameterCount)
                    .DefaultIfEmpty().Max();
                // Greedy estimate for maximum possible parameter match.
                //  there need to be enough space for any rule's parameters to fit, since we don't know which ones will match.
                var maximumPossibleParameterMatch = maxParamCapturedByAnyRule;
                if (maximumPossibleParameterMatch > ushort.MaxValue)
                {
                    throw new LSystemRuntimeException($"Rules with more than {ushort.MaxValue} captured local parameters over all conditional options");
                }
                maxMemoryRequirementsPerSymbol[symbol] = new MaxMatchMemoryRequirements
                {
                    maxParameters = maximumPossibleParameterMatch
                };
            }

            nativeRuleData.blittableRulesByTargetSymbol = NativeOrderedMultiDictionary<BasicRule.Blittable>.WithMapFunction(
                rulesByTargetSymbol,
                rule => rule.AsBlittable(),
                Allocator.Persistent);
            nativeRuleData.maxParameterMemoryRequirementsPerSymbol = maxMemoryRequirementsPerSymbol;
            this.nativeRuleData = new DependencyTracker<SystemLevelRuleNativeData>(nativeRuleData);
        }
        public LSystemState<float> StepSystem(LSystemState<float> systemState, float[] globalParameters = null, bool disposeOldSystem = true)
        {
#if UNITY_EDITOR
            try
            {
#endif
                var stepper = StepSystemJob(systemState, globalParameters);
                while (!stepper.IsComplete())
                {
                    stepper = stepper.StepNextTyped();
                }
                if (disposeOldSystem)
                {
                    systemState.currentSymbols.Dispose();
                }
                if (stepper.HasErrored())
                {
                    throw new LSystemRuntimeException("Error during stepping");
                }
                return stepper.GetData();
#if UNITY_EDITOR
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                throw;
            }
#endif
        }

        /// <summary>
        /// Step the given <paramref name="systemState"/>. returning the new system state. No modifications are made the the system sate
        /// Rough system step process:
        ///     1. iterate through the current system state, retrieving the maximum # of parameters that can be captured for each symbol.
        ///         Track symbols with conditionals seperately. during the match phase, every rule which is a conditional will match if possible
        ///         and if no higher-ranking rule has matched yet. Allocate memory for the possible match array, and the parameter match array
        ///     2. batch process each potential match, and each possible conditional. stop processing rules for a specific symbol once any non-conditional
        ///         rule matches. write all matched parameters for every matched rule into temporary parameter memory space.
        ///     3. Match selection: For each symbol, iterate through the possible matches identified in #2. When the first match is found, populate info about the selected
        ///         match and the size of the replacement into singleton
        ///     4. Sum up the new symbol string length, and allocate memory for it.
        ///     5. Batch process each symbol in parallel again, this time writing the replacement symbols themselves into memory. Will rely on reading parameters
        ///         out of the memory allocated in
        /// </summary>
        /// <param name="systemState">The entire state of the L-system. no modifications are made to this object or the contained properties.</param>
        /// <param name="globalParameters">The global parameters, if any</param>
        public ICompletable<LSystemState<float>> StepSystemJob(LSystemState<float> systemState, float[] globalParameters = null, JobHandle parameterWriteDependency = default)
        {
            if (isDisposed)
            {
                Debug.LogError($"LSystem has already been disposed");
                return null;
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

            var result = new LSystemParameterSizeCountingCompletable(
                systemState,
                nativeRuleData,
                globalParameters,
                includedCharacters,
                customSymbols,
                parameterWriteDependency);
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
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
            nativeRuleData.Dispose();
        }
    }
}
