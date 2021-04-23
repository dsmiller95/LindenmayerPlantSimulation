using Dman.LSystem.SystemCompiler;
using LSystem.Runtime.SystemRuntime;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace Dman.LSystem.SystemRuntime
{
    public class BasicRule
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int TargetSymbol => _targetSymbolWithParameters.targetSymbol;

        public SymbolSeriesMatcher ContextPrefix {get; private set;}

        public SymbolSeriesMatcher ContextSuffix { get; private set; }

        public int CapturedLocalParameterCount { get; private set; }

        private readonly InputSymbol _targetSymbolWithParameters;
        private System.Delegate conditionalChecker;

        public RuleOutcome[] possibleOutcomes;

        public BasicRule(ParsedRule parsedInfo, int branchOpenSymbol = '[', int branchCloseSymbol = ']')
        {
            _targetSymbolWithParameters = parsedInfo.coreSymbol;
            possibleOutcomes = new RuleOutcome[] {
                new RuleOutcome(1, parsedInfo.replacementSymbols)
            };

            conditionalChecker = parsedInfo.conditionalMatch;
            ContextPrefix = new SymbolSeriesMatcher(parsedInfo.backwardsMatch);
            var suffix = new SymbolSeriesMatcher(parsedInfo.forwardsMatch);
            // property getters do weird things when they are backing structs
            suffix.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);
            ContextSuffix = suffix;

            CapturedLocalParameterCount = _targetSymbolWithParameters.parameterLength +
                ContextPrefix.targetSymbolSeries.Sum(x => x.parameterLength) +
                ContextSuffix.targetSymbolSeries.Sum(x => x.parameterLength);
        }
        /// <summary>
        /// Create a new basic rule with multiple random outcomes.
        /// It is garenteed and required that all of the stochastic rules will capture the
        /// same parameters
        /// </summary>
        /// <param name="parsedRules"></param>
        public BasicRule(IEnumerable<ParsedStochasticRule> parsedRules, int branchOpenSymbol = '[', int branchCloseSymbol = ']')
        {
            possibleOutcomes = parsedRules
                .Select(x =>
                    new RuleOutcome(x.probability, x.replacementSymbols)
                ).ToArray();
            var firstOutcome = parsedRules.First();
            _targetSymbolWithParameters = firstOutcome.coreSymbol;

            conditionalChecker = firstOutcome.conditionalMatch;
            ContextPrefix = new SymbolSeriesMatcher(firstOutcome.backwardsMatch);
            var suffix = new SymbolSeriesMatcher(firstOutcome.forwardsMatch);
            // property getters do weird things when they are backing structs
            suffix.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);
            ContextSuffix = suffix;

            CapturedLocalParameterCount = _targetSymbolWithParameters.parameterLength +
                ContextPrefix.targetSymbolSeries.Sum(x => x.parameterLength) +
                ContextSuffix.targetSymbolSeries.Sum(x => x.parameterLength);
        }

        //public Blittable AsBlittable()
        //{
        //    return new Blittable
        //    {
        //        contextPrefix = ContextPrefix,
        //        contextSuffix = ContextSuffix,
        //        targetSymbolWithParameters = _targetSymbolWithParameters.AsBlittable(),
        //        capturedLocalParameterCount = CapturedLocalParameterCount,
        //        possibleOutcomes = possibleOutcomes.Select(x => x.AsBlittable()).ToArray(),
        //        hasConditional = conditionalChecker != null
        //    };
        //}

        //public struct Blittable
        //{
        //    public SymbolSeriesMatcher contextPrefix;
        //    public SymbolSeriesMatcher contextSuffix;
        //    public InputSymbol.Blittable targetSymbolWithParameters;
        //    public int capturedLocalParameterCount;
        //    public RuleOutcome.Blittable[] possibleOutcomes;
        //    public bool hasConditional;

        //    private byte SelectOutcomeIndex(ref Unity.Mathematics.Random rand)
        //    {
        //        if (possibleOutcomes.Length > 1)
        //        {
        //            var sample = rand.NextDouble();
        //            double currentPartition = 0;
        //            for (byte i = 0; i < possibleOutcomes.Length; i++)
        //            {
        //                var possibleOutcome = possibleOutcomes[i];
        //                currentPartition += possibleOutcome.probability;
        //                if (sample <= currentPartition)
        //                {
        //                    return i;
        //                }
        //            }
        //            throw new LSystemRuntimeException("possible outcome probabilities do not sum to 1");
        //        }
        //        return 0;
        //    }

        //    public bool PreMatchCapturedParametersWithoutConditional(
        //        SymbolStringBranchingCache branchingCache,
        //        SymbolString<float> source,
        //        int indexInSymbols,
        //        NativeArray<float> parameterMemory,
        //        ref LSystemSingleSymbolMatchData matchSingletonData)
        //    {
        //        var target = targetSymbolWithParameters;

        //        var parameterStartIndex = matchSingletonData.tmpParameterMatchStartIndex;

        //        // parameters
        //        byte matchedParameterNum = 0;

        //        // context match
        //        if (contextPrefix.IsCreated && contextPrefix.targetSymbolSeries.Length > 0)
        //        {
        //            var backwardMatchMapping = branchingCache.MatchesBackwards(
        //                indexInSymbols,
        //                contextPrefix,
        //                source.symbols,
        //                source.newParameters.indexing
        //                );
        //            if (backwardMatchMapping == null)
        //            {
        //                // if backwards match exists, and does not match, then fail this match attempt.
        //                return false;
        //            }
        //            for (int indexInPrefix = 0; indexInPrefix < contextPrefix.targetSymbolSeries.Length; indexInPrefix++)
        //            {
        //                if (!backwardMatchMapping.TryGetValue(indexInPrefix, out var matchingTargetIndex))
        //                {
        //                    continue;
        //                }
        //                var parametersIndexing = source.newParameters[matchingTargetIndex];
        //                for (int i = 0; i < parametersIndexing.length; i++)
        //                {
        //                    var paramValue = source.newParameters[parametersIndexing, i];

        //                    parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
        //                    matchedParameterNum++;
        //                }
        //            }
        //        }

        //        var coreParametersIndexing = source.newParameters[indexInSymbols];
        //        if (coreParametersIndexing.length != target.parameterLength)
        //        {
        //            return false;
        //        }
        //        if (coreParametersIndexing.length > 0)
        //        {
        //            for (int i = 0; i < coreParametersIndexing.length; i++)
        //            {
        //                var paramValue = source.newParameters[coreParametersIndexing, i];

        //                parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
        //                matchedParameterNum++;
        //            }
        //        }

        //        if (contextSuffix.IsCreated && contextSuffix.targetSymbolSeries.Length > 0)
        //        {
        //            var forwardMatch = branchingCache.MatchesForward(
        //                indexInSymbols,
        //                contextSuffix,
        //                source.symbols,
        //                source.newParameters.indexing);
        //            if (forwardMatch == null)
        //            {
        //                // if forwards match exists, and does not match, then fail this match attempt.
        //                return false;
        //            }
        //            for (int indexInSuffix = 0; indexInSuffix < contextSuffix.targetSymbolSeries.Length; indexInSuffix++)
        //            {
        //                if (!forwardMatch.TryGetValue(indexInSuffix, out var matchingTargetIndex))
        //                {
        //                    continue;
        //                }

        //                var parametersIndexing = source.newParameters[matchingTargetIndex];
        //                for (int i = 0; i < parametersIndexing.length; i++)
        //                {
        //                    var paramValue = source.newParameters[parametersIndexing, i];

        //                    parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
        //                    matchedParameterNum++;
        //                }
        //            }
        //        }

        //        // stochastic selection
        //        matchSingletonData.tmpParameterMatchedCount = matchedParameterNum;
        //        matchSingletonData.replacementSymbolLength = (ushort)outcomeObject.replacementSymbolSize;
        //        matchSingletonData.replacementParameterCount = outcomeObject.replacementParameterCount;

        //        return true;
        //    }
        //}

        public bool PreMatchCapturedParameters(
            SymbolStringBranchingCache branchingCache,
            SymbolString<float> source,
            int indexInSymbols,
            NativeArray<float> globalParameters,
            NativeArray<float> parameterMemory,
            ref Unity.Mathematics.Random random,
            ref LSystemSingleSymbolMatchData matchSingletonData)
        {
            var target = _targetSymbolWithParameters;

            var parameterStartIndex = matchSingletonData.tmpParameterMatchStartIndex;

            // parameters
            byte matchedParameterNum = 0;

            // context match
            if (ContextPrefix.IsCreated && ContextPrefix.targetSymbolSeries.Length > 0)
            {
                var backwardMatchMapping = branchingCache.MatchesBackwards(
                    indexInSymbols,
                    ContextPrefix,
                    source.symbols,
                    source.newParameters.indexing
                    );
                if (backwardMatchMapping == null)
                {
                    // if backwards match exists, and does not match, then fail this match attempt.
                    return false;
                }
                for (int indexInPrefix = 0; indexInPrefix < ContextPrefix.targetSymbolSeries.Length; indexInPrefix++)
                {
                    if (!backwardMatchMapping.TryGetValue(indexInPrefix, out var matchingTargetIndex))
                    {
                        continue;
                    }
                    var parametersIndexing = source.newParameters[matchingTargetIndex];
                    for (int i = 0; i < parametersIndexing.length; i++)
                    {
                        var paramValue = source.newParameters[parametersIndexing, i];

                        parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
                        matchedParameterNum++;
                    }
                }
            }


            var coreParametersIndexing = source.newParameters[indexInSymbols];
            if (coreParametersIndexing.length != target.parameterLength)
            {
                return false;
            }
            if (coreParametersIndexing.length > 0)
            {
                for (int i = 0; i < coreParametersIndexing.length; i++)
                {
                    var paramValue = source.newParameters[coreParametersIndexing, i];

                    parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
                    matchedParameterNum++;
                }
            }

            if (ContextSuffix.IsCreated && ContextSuffix.targetSymbolSeries.Length > 0)
            {
                var forwardMatch = branchingCache.MatchesForward(
                    indexInSymbols,
                    ContextSuffix,
                    source.symbols,
                    source.newParameters.indexing);
                if (forwardMatch == null)
                {
                    // if forwards match exists, and does not match, then fail this match attempt.
                    return false;
                }
                for (int indexInSuffix = 0; indexInSuffix < ContextSuffix.targetSymbolSeries.Length; indexInSuffix++)
                {
                    if (!forwardMatch.TryGetValue(indexInSuffix, out var matchingTargetIndex))
                    {
                        continue;
                    }

                    var parametersIndexing = source.newParameters[matchingTargetIndex];
                    for (int i = 0; i < parametersIndexing.length; i++)
                    {
                        var paramValue = source.newParameters[parametersIndexing, i];

                        parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
                        matchedParameterNum++;
                    }
                }
            }

            // conditional
            if (conditionalChecker != null)
            {
                var paramArray = new object[globalParameters.Length + matchedParameterNum];
                for (int i = 0; i < globalParameters.Length; i++)
                {
                    paramArray[i] = globalParameters[i];
                }
                for (int i = 0; i < matchedParameterNum; i++)
                {
                    paramArray[i + globalParameters.Length] = parameterMemory[parameterStartIndex + i];
                }
                var invokeResult = conditionalChecker.DynamicInvoke(paramArray);
                if (!(invokeResult is bool boolResult))
                {
                    // TODO: call this out a bit better. All compilation context is lost here
                    throw new LSystemRuntimeException($"Conditional expression must evaluate to a boolean");
                }
                var conditionalResult = boolResult;
                if (!conditionalResult)
                {
                    return false;
                }
            }

            // stochastic selection
            matchSingletonData.selectedReplacementPattern = SelectOutcomeIndex(ref random);
            var outcomeObject = possibleOutcomes[matchSingletonData.selectedReplacementPattern];
            matchSingletonData.tmpParameterMatchedCount = matchedParameterNum;

            matchSingletonData.replacementSymbolLength = outcomeObject.ReplacementSymbolCount();
            matchSingletonData.replacementParameterCount = outcomeObject.ReplacementParameterCount();

            return true;
        }
        private byte SelectOutcomeIndex(ref Unity.Mathematics.Random rand)
        {
            if (possibleOutcomes.Length > 1)
            {
                var sample = rand.NextDouble();
                double currentPartition = 0;
                for (byte i = 0; i < possibleOutcomes.Length; i++)
                {
                    var possibleOutcome = possibleOutcomes[i];
                    currentPartition += possibleOutcome.probability;
                    if (sample <= currentPartition)
                    {
                        return i;
                    }
                }
                throw new LSystemRuntimeException("possible outcome probabilities do not sum to 1");
            }
            return 0;
        }

        public void WriteReplacementSymbols(
            NativeArray<float> globalParameters,
            NativeArray<float> paramTempMemorySpace,
            SymbolString<float> target,
            LSystemSingleSymbolMatchData matchSingletonData)
        {
            var selectedReplacementPattern = matchSingletonData.selectedReplacementPattern;

            var indexInMatchedParameters = matchSingletonData.tmpParameterMatchStartIndex;
            var totalMatchedParameters = matchSingletonData.tmpParameterMatchedCount;

            var indexInReplacementSymbols = matchSingletonData.replacementSymbolStartIndex;
            var expectedReplacementSymbolLength = matchSingletonData.replacementSymbolLength;

            var indexInReplacementParameters = matchSingletonData.replacementParameterStartIndex;
            var expectedReplacementParameterLength = matchSingletonData.replacementParameterCount;

            var orderedMatchedParameters = new object[globalParameters.Length + totalMatchedParameters];
            for (int i = 0; i < globalParameters.Length; i++)
            {
                orderedMatchedParameters[i] = globalParameters[i];
            }
            for (int i = 0; i < totalMatchedParameters; i++)
            {
                orderedMatchedParameters[globalParameters.Length + i] = paramTempMemorySpace[indexInMatchedParameters + i];
            }
            var outcome = possibleOutcomes[selectedReplacementPattern];

            var replacement = outcome.GenerateReplacement(orderedMatchedParameters);
            if(replacement.Length != expectedReplacementSymbolLength)
            {
                throw new System.Exception("Unexpected state: replacement symbol size differs from expected");
            }
            if (replacement.newParameters.data.Length != expectedReplacementParameterLength)
            {
                throw new System.Exception("Unexpected state: replacement paremeter size differs from expected");
            }

            target.CopyFrom(replacement, indexInReplacementSymbols, indexInReplacementParameters);
        }
    }
}
