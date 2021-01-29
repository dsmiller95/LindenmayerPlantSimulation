using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem
{
    public class BasicRule : IRule<double>
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int[] TargetSymbolSeries => _targetSymbols;
        private readonly int[] _targetSymbols;

        private readonly SingleSymbolMatcher[] _targetSymbolsWithParameters;
        private System.Delegate conditionalChecker;

        public RuleOutcome[] possibleOutcomes;


        //public BasicRule(int[] targetSymbols, RuleOutcome[] outcomes)
        //{
        //    this._targetSymbols = targetSymbols;
        //    this.possibleOutcomes = outcomes;
        //}

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
        /// <param name="parameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString<double> ApplyRule(System.ArraySegment<double[]> parameters, System.Random random)
        {
            var orderedMatchedParameters = new List<object>();
            for (int targetSymbolIndex = 0; targetSymbolIndex < _targetSymbolsWithParameters.Length; targetSymbolIndex++)
            {
                var target = _targetSymbolsWithParameters[targetSymbolIndex];
                var parameter = parameters.Array[parameters.Offset + targetSymbolIndex];
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
                var conditionalResult = (bool)invokeResult;
                if (!conditionalResult)
                {
                    return null;
                }
            }


            RuleOutcome outcome = default;
            if(this.possibleOutcomes.Length > 1)
            {
                var sample = random.NextDouble();
                double currentPartition = 0;
                foreach (var possibleOutcome in possibleOutcomes)
                {
                    currentPartition += possibleOutcome.probability;
                    if(sample <= currentPartition)
                    {
                        outcome = possibleOutcome;
                        break;
                    }
                }
                if (outcome.replacementSymbols == null)
                {
                    throw new System.Exception("possible outcome probabilities do not sum to 1");
                }
            }else
            {
                outcome = possibleOutcomes[0];
            }

            return outcome.GenerateReplacement(paramArray);
        }
    }
}
