using Dman.LSystem.SystemCompiler;
using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{
    internal class BasicRule : IRule<double>
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int TargetSymbol => _targetSymbolWithParameters.targetSymbol;

        public SymbolSeriesMatcher ContextPrefix {get; private set;}

        public SymbolSeriesMatcher ContextSuffix { get; private set; }


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
        }
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
        }

        /// <summary>
        /// retrun the symbol string to replace the given symbol with. return null if no match
        /// </summary>
        /// <param name="symbol">the symbol to be replaced</param>
        /// <param name="symbolParameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString<double> ApplyRule(
            SymbolStringBranchingCache branchingCache,
            SymbolString<double> symbols,
            int indexInSymbols,
            ref Unity.Mathematics.Random random,
            double[] globalRuntimeParameters = null)
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
            if (ContextPrefix != null)
            {
                var backwardMatch = branchingCache.MatchesBackwards(indexInSymbols, ContextSuffix);
                if (backwardMatch == null)
                {
                    return null;
                }
                // TODO: get parameters
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

            if (ContextSuffix != null)
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

                //TODO: get parameters
            }



            var paramArray = orderedMatchedParameters.ToArray();

            // conditional
            if (conditionalChecker != null)
            {
                var invokeResult = conditionalChecker.DynamicInvoke(paramArray);
                if (!(invokeResult is bool boolResult))
                {
                    // TODO: call this out a bit better. All compilation context is lost here
                    throw new System.Exception($"Conditional expression must evaluate to a boolean");
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
                throw new System.Exception("possible outcome probabilities do not sum to 1");
            }
            return possibleOutcomes[0];
        }
    }
}
