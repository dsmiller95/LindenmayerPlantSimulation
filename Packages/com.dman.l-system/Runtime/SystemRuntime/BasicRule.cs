using Dman.LSystem.SystemCompiler;
using LSystem.Runtime.SystemRuntime;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace Dman.LSystem.SystemRuntime
{
    internal class BasicRule : IRule<float>
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
            ContextSuffix = new SymbolSeriesMatcher(parsedInfo.forwardsMatch);
            ContextSuffix.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);

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
            ContextSuffix = new SymbolSeriesMatcher(firstOutcome.forwardsMatch);
            ContextSuffix.ComputeGraphIndexes(branchOpenSymbol, branchCloseSymbol);

            CapturedLocalParameterCount = _targetSymbolWithParameters.parameterLength +
                ContextPrefix.targetSymbolSeries.Sum(x => x.parameterLength) +
                ContextSuffix.targetSymbolSeries.Sum(x => x.parameterLength);
        }

        private RuleOutcome SelectOutcome(ref Unity.Mathematics.Random rand)
        {
            if (possibleOutcomes.Length > 1)
            {
                var sample = rand.NextDouble();
                double currentPartition = 0;
                foreach (var possibleOutcome in possibleOutcomes)
                {
                    currentPartition += possibleOutcome.probability;
                    if (sample <= currentPartition)
                    {
                        return possibleOutcome;
                    }
                }
                throw new LSystemRuntimeException("possible outcome probabilities do not sum to 1");
            }
            return possibleOutcomes[0];
        }

        public bool PreMatchCapturedParameters(
            SymbolStringBranchingCache branchingCache,
            SymbolString<float> source,
            int indexInSymbols,
            NativeArray<float> globalParameters,
            NativeArray<float> parameterMemory,
            ref Unity.Mathematics.Random random,
            ref LSystemStepMatchIntermediate matchSingletonData)
        {
            var target = _targetSymbolWithParameters;

            var parameterStartIndex = matchSingletonData.parametersStartIndex;

            // parameters
            byte matchedParameterNum = 0;

            // context match
            if (ContextPrefix != null && ContextPrefix.targetSymbolSeries?.Length > 0)
            {
                var backwardMatchMapping = branchingCache.MatchesBackwards(
                    indexInSymbols,
                    ContextPrefix,
                    source.symbols,
                    source.parameterIndexes
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
                    var parametersIndexing = source.parameterIndexes[matchingTargetIndex];
                    for (int i = parametersIndexing.Start; i < parametersIndexing.End; i++)
                    {
                        var paramValue = source.parameters[i];

                        parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
                        matchedParameterNum++;
                    }
                }
            }


            var coreParametersIndexing = source.parameterIndexes[indexInSymbols];
            if (coreParametersIndexing.length != target.parameterLength)
            {
                return false;
            }
            if (coreParametersIndexing.length > 0)
            {
                for (int i = coreParametersIndexing.Start; i < coreParametersIndexing.End; i++)
                {
                    var paramValue = source.parameters[i];

                    parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
                    matchedParameterNum++;
                }
            }

            if (ContextSuffix != null && ContextSuffix.targetSymbolSeries?.Length > 0)
            {
                var forwardMatch = branchingCache.MatchesForward(
                    indexInSymbols,
                    ContextSuffix,
                    source.symbols,
                    source.parameterIndexes);
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

                    var parametersIndexing = source.parameterIndexes[matchingTargetIndex];
                    for (int i = parametersIndexing.Start; i < parametersIndexing.End; i++)
                    {
                        var paramValue = source.parameters[i];

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
            matchSingletonData.matchedParametersCount = matchedParameterNum;

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
            LSystemStepMatchIntermediate matchSingletonData)
        {
            var selectedReplacementPattern = matchSingletonData.selectedReplacementPattern;

            var indexInMatchedParameters = matchSingletonData.parametersStartIndex;
            var totalMatchedParameters = matchSingletonData.matchedParametersCount;

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
            if (replacement.parameters.Length != expectedReplacementParameterLength)
            {
                throw new System.Exception("Unexpected state: replacement paremeter size differs from expected");
            }

            target.CopyFrom(replacement, indexInReplacementSymbols, indexInReplacementParameters);
        }
    }
}
