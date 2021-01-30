using Dman.LSystem.SystemCompiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime
{
    public class BasicRule : IRule<double>
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int[] TargetSymbolSeries => _targetSymbols;
        private readonly int[] _targetSymbols;

        private readonly InputSymbol[] _targetSymbolsWithParameters;
        private System.Delegate conditionalChecker;

        public RuleOutcome[] possibleOutcomes;

        public BasicRule(ParsedRule parsedInfo)
        {
            _targetSymbolsWithParameters = parsedInfo.targetSymbols;
            _targetSymbols = _targetSymbolsWithParameters.Select(x => x.targetSymbol).ToArray();
            conditionalChecker = parsedInfo.conditionalMatch;
            possibleOutcomes = new RuleOutcome[] {
                new RuleOutcome
                {
                    probability = 1,
                    replacementSymbols = parsedInfo.replacementSymbols
                }
            };  
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
            _targetSymbolsWithParameters = firstOutcome.targetSymbols;
            _targetSymbols = _targetSymbolsWithParameters.Select(x => x.targetSymbol).ToArray();

            conditionalChecker = firstOutcome.conditionalMatch;
        }

        /// <summary>
        /// retrun the symbol string to replace the given symbol with. return null if no match
        /// </summary>
        /// <param name="symbol">the symbol to be replaced</param>
        /// <param name="symbolParameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString<double> ApplyRule(
            System.ArraySegment<double[]> symbolParameters,
            System.Random random,
            double[] globalParameters = null)
        {
            var orderedMatchedParameters =  new List<object>();
            if(globalParameters != null)
            {
                foreach (var globalParam in globalParameters)
                {
                    orderedMatchedParameters.Add(globalParam);
                }
            }
            for (int targetSymbolIndex = 0; targetSymbolIndex < _targetSymbolsWithParameters.Length; targetSymbolIndex++)
            {
                var target = _targetSymbolsWithParameters[targetSymbolIndex];
                var parameter = symbolParameters.Array[symbolParameters.Offset + targetSymbolIndex];
                if(parameter == null)
                {
                    if (target.namedParameters.Length > 0)
                    {
                        return null;
                    }
                    continue;
                }
                if(target.namedParameters.Length != parameter.Length)
                {
                    return null;
                }
                for (int parameterIndex = 0; parameterIndex < parameter.Length; parameterIndex++)
                {
                    orderedMatchedParameters.Add(parameter[parameterIndex]);
                }
            }

            var paramArray = orderedMatchedParameters.ToArray();

            if(conditionalChecker != null)
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


            RuleOutcome outcome = SelectOutcome(random);

            return outcome.GenerateReplacement(paramArray);
        }

        private RuleOutcome SelectOutcome(System.Random rand)
        {
            if (this.possibleOutcomes.Length > 1)
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
