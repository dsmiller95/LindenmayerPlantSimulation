using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem
{
    public struct RuleOutcome
    {
        public float probability;
        public int[] replacementSymbols;
    }

    public class BasicRule :IRule
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int TargetSymbol => _targetSymbol;
        private readonly int _targetSymbol;

        public RuleOutcome[] possibleOutcomes;

        public BasicRule(ParsedRule parsedInfo)
        {
            _targetSymbol = parsedInfo.targetSymbol;
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
            _targetSymbol = parsedRules.First().targetSymbol;
        }

        /// <summary>
        /// retrun the symbol string to replace the given symbol with. return null if no match
        /// </summary>
        /// <param name="symbol">the symbol to be replaced</param>
        /// <param name="parameters">the parameters applied to the symbol. Could be null if no parameters.</param>
        /// <returns></returns>
        public SymbolString ApplyRule(float[] parameters, System.Random random)
        {
            if(parameters != null && parameters.Length > 0)
            {
                return null;
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

            return new SymbolString(outcome.replacementSymbols);
        }
    }
}
