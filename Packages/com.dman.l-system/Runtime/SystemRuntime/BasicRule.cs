using Dman.LSystem.SystemCompiler;
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

        public BasicRule(ParsedRule parsedInfo)
        {
            _targetSymbolWithParameters = parsedInfo.coreSymbol;
            possibleOutcomes = new RuleOutcome[] {
                new RuleOutcome
                {
                    probability = 1,
                    replacementSymbols = parsedInfo.replacementSymbols
                }
            };

            conditionalChecker = parsedInfo.conditionalMatch;
            ContextPrefix = new SymbolSeriesMatcher(parsedInfo.backwardsMatch);
            ContextSuffix = new SymbolSeriesMatcher(parsedInfo.forwardsMatch);

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
        public BasicRule(IEnumerable<ParsedStochasticRule> parsedRules)
        {
            possibleOutcomes = parsedRules
                .Select(x => new RuleOutcome
                {
                    probability = x.probability,
                    replacementSymbols = x.replacementSymbols
                }).ToArray();
            var firstOutcome = parsedRules.First();
            _targetSymbolWithParameters = firstOutcome.coreSymbol;

            conditionalChecker = firstOutcome.conditionalMatch;
            ContextPrefix = new SymbolSeriesMatcher(firstOutcome.backwardsMatch);
            ContextSuffix = new SymbolSeriesMatcher(firstOutcome.forwardsMatch);

            CapturedLocalParameterCount = _targetSymbolWithParameters.parameterLength +
                ContextPrefix.targetSymbolSeries.Sum(x => x.parameterLength) +
                ContextSuffix.targetSymbolSeries.Sum(x => x.parameterLength);
        }

        /// <summary>
        /// retrun the symbol string to replace the given symbol with. return null if no match
        /// </summary>
        /// <param name="symbol">the symbol to be replaced</param>
        /// <param name="symbolParameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString<float> ApplyRule(
            SymbolStringBranchingCache branchingCache,
            SymbolString<float> symbols,
            int indexInSymbols,
            ref Unity.Mathematics.Random random,
            float[] globalRuntimeParameters = null)
        {
            var target = _targetSymbolWithParameters;

            // parameters
            var orderedMatchedParameters = new List<object>();
            if (globalRuntimeParameters != null)
            {
                foreach (var globalParam in globalRuntimeParameters)
                {
                    orderedMatchedParameters.Add(globalParam);
                }
            }

            // context match
            if (ContextPrefix != null && ContextPrefix.targetSymbolSeries?.Length > 0)
            {
                var backwardMatchMapping = branchingCache.MatchesBackwards(indexInSymbols, ContextPrefix);
                if (backwardMatchMapping == null)
                {
                    return null;
                }
                for (int indexInPrefix = 0; indexInPrefix < ContextPrefix.targetSymbolSeries.Length; indexInPrefix++)
                {
                    if (!backwardMatchMapping.TryGetValue(indexInPrefix, out var matchingTargetIndex))
                    {
                        continue;
                    }
                    var nextSymbol = symbols.parameters[matchingTargetIndex];
                    foreach (var paramValue in nextSymbol)
                    {
                        orderedMatchedParameters.Add(paramValue);
                    }
                }
            }

            var coreParameter = symbols.parameters[indexInSymbols];
            if ((coreParameter?.Length ?? 0) != target.parameterLength)
            {
                return null;
            }
            if (coreParameter != null)
            {
                for (int parameterIndex = 0; parameterIndex < coreParameter.Length; parameterIndex++)
                {
                    orderedMatchedParameters.Add(coreParameter[parameterIndex]);
                }
            }

            if (ContextSuffix != null && ContextSuffix.targetSymbolSeries?.Length > 0)
            {
                var forwardMatch = branchingCache.MatchesForward(indexInSymbols, ContextSuffix, false);
                if (forwardMatch == null)
                {
                    return null;
                }
                for (int indexInSuffix = 0; indexInSuffix < ContextSuffix.targetSymbolSeries.Length; indexInSuffix++)
                {
                    if(!forwardMatch.TryGetValue(indexInSuffix, out var matchingTargetIndex))
                    {
                        continue;
                    }
                    var nextSymbol = symbols.parameters[matchingTargetIndex];
                    foreach (var paramValue in nextSymbol)
                    {
                        orderedMatchedParameters.Add(paramValue);
                    }
                }
            }



            var paramArray = orderedMatchedParameters.ToArray();

            // conditional
            if (conditionalChecker != null)
            {
                var invokeResult = conditionalChecker.DynamicInvoke(paramArray);
                if (!(invokeResult is bool boolResult))
                {
                    // TODO: call this out a bit better. All compilation context is lost here
                    throw new LSystemRuntimeException($"Conditional expression must evaluate to a boolean");
                }
                var conditionalResult = boolResult;
                if (!conditionalResult)
                {
                    return null;
                }
            }

            // stochastic selection
            RuleOutcome outcome = SelectOutcome(ref random);

            // replacement
            return outcome.GenerateReplacement(paramArray);
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

        public float[] PreMatchCapturedParameters(
            SymbolStringBranchingCache branchingCache,
            SymbolString<float> symbols,
            int indexInSymbols,
            float[] globalParameters,
            ref Unity.Mathematics.Random random,
            out ushort replacementSymbolLength,
            out byte selectedReplacementPattern)
        {
            replacementSymbolLength = 0;
            selectedReplacementPattern = 0;

            var target = _targetSymbolWithParameters;

            // parameters
            var orderedMatchedParameters = new List<float>();

            // context match
            if (ContextPrefix != null && ContextPrefix.targetSymbolSeries?.Length > 0)
            {
                var backwardMatchMapping = branchingCache.MatchesBackwards(indexInSymbols, ContextPrefix);
                if (backwardMatchMapping == null)
                {
                    // if backwards match exists, and does not match, then fail this match attempt.
                    return null;
                }
                for (int indexInPrefix = 0; indexInPrefix < ContextPrefix.targetSymbolSeries.Length; indexInPrefix++)
                {
                    if (!backwardMatchMapping.TryGetValue(indexInPrefix, out var matchingTargetIndex))
                    {
                        continue;
                    }
                    var nextSymbol = symbols.parameters[matchingTargetIndex];
                    foreach (var paramValue in nextSymbol)
                    {
                        orderedMatchedParameters.Add(paramValue);
                    }
                }
            }

            var coreParameter = symbols.parameters[indexInSymbols];
            if ((coreParameter?.Length ?? 0) != target.parameterLength)
            {
                // if core symbol doesn't have the same number of parameters as the target core symbol
                //  then fail the match attempt
                return null;
            }
            if (coreParameter != null)
            {
                for (int parameterIndex = 0; parameterIndex < coreParameter.Length; parameterIndex++)
                {
                    orderedMatchedParameters.Add(coreParameter[parameterIndex]);
                }
            }

            if (ContextSuffix != null && ContextSuffix.targetSymbolSeries?.Length > 0)
            {
                var forwardMatch = branchingCache.MatchesForward(indexInSymbols, ContextSuffix, false);
                if (forwardMatch == null)
                {
                    // if forwards match exists, and does not match, then fail this match attempt.
                    return null;
                }
                for (int indexInSuffix = 0; indexInSuffix < ContextSuffix.targetSymbolSeries.Length; indexInSuffix++)
                {
                    if (!forwardMatch.TryGetValue(indexInSuffix, out var matchingTargetIndex))
                    {
                        continue;
                    }
                    var nextSymbol = symbols.parameters[matchingTargetIndex];
                    foreach (var paramValue in nextSymbol)
                    {
                        orderedMatchedParameters.Add(paramValue);
                    }
                }
            }


            var paramArray = orderedMatchedParameters.ToArray();

            // conditional
            if (conditionalChecker != null)
            {
                var invokeResult = conditionalChecker.DynamicInvoke(globalParameters.Concat(paramArray).Cast<object>().ToArray());
                if (!(invokeResult is bool boolResult))
                {
                    // TODO: call this out a bit better. All compilation context is lost here
                    throw new LSystemRuntimeException($"Conditional expression must evaluate to a boolean");
                }
                var conditionalResult = boolResult;
                if (!conditionalResult)
                {
                    return null;
                }
            }

            // stochastic selection
            selectedReplacementPattern = SelectOutcomeIndex(ref random);
            var outcomeObject = possibleOutcomes[selectedReplacementPattern];
            replacementSymbolLength = outcomeObject.ReplacementSymbolSize();

            return paramArray;
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
            float[] globalParameters,
            byte selectedReplacementPattern,
            NativeArray<float> parameters,
            int originIndexInParameters,
            int totalMatchedParameters,
            SymbolString<float> targetSymbols,
            int originIndexInSymbols,
            ushort expectedReplacementSymbolLength)
        {
            var orderedMatchedParameters = new object[globalParameters.Length + totalMatchedParameters];
            for (int i = 0; i < globalParameters.Length; i++)
            {
                orderedMatchedParameters[i] = globalParameters[i];
            }
            for (int i = 0; i < totalMatchedParameters; i++)
            {
                orderedMatchedParameters[globalParameters.Length + i] = parameters[originIndexInParameters + i];
            }
            var outcome = possibleOutcomes[selectedReplacementPattern];

            var replacement = outcome.GenerateReplacement(orderedMatchedParameters.ToArray());
            if(replacement.Length != expectedReplacementSymbolLength)
            {
                throw new System.Exception("Unexpected state: replacement symbol size differs from expected");
            }
            for (int i = 0; i < replacement.Length; i++)
            {
                targetSymbols.symbols[originIndexInSymbols + i] = replacement.symbols[i];
                targetSymbols.parameters[originIndexInSymbols + i] = replacement.parameters[i];
            }
            return;
        }
    }
}
