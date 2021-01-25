using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem
{
    public class ParsedStochasticRule : ParsedRule
    {
        public float probability;
    }

    public class ParsedRule
    {
        public int targetSymbol;
        public int[] replacementSymbols;

        /// <summary>
        /// builds a rule based on the string definition, of format:
        ///   "A -> BACCB"
        ///   first char is always the target character
        ///   "->" delimits between target char and replacement string
        ///   everything after "->" is the replacement string
        /// </summary>
        /// <param name="ruleDef"></param>
        public static ParsedRule ParseToRule(string ruleString)
        {
            var ruleMatch = Regex.Match(ruleString.Trim(), @"(?<target>\w)\s*(?:\(P(?<probability>[.0-9]+)\))?\s*->\s*(?<replacement>.+)");
            ParsedRule rule;
            if (ruleMatch.Groups["probability"].Success)
            {
                rule = new ParsedStochasticRule
                {
                    probability = float.Parse(ruleMatch.Groups["probability"].Value)
                };
            }else
            {
                rule = new ParsedRule();
            }
            rule.targetSymbol = ruleMatch.Groups["target"].Value[0];
            rule.replacementSymbols = ruleMatch.Groups["replacement"].Value.ToIntArray();
            return rule;
        }

        public static IEnumerable<IRule> CompileRules(IEnumerable<string> ruleStrings) {
            var parsedRules = ruleStrings.Select(x => ParseToRule(x)).ToArray();
            var basicRules = parsedRules.Where(r => !(r is ParsedStochasticRule))
                .Select(x => new BasicRule(x));

            var stochasticRules = parsedRules.Where(r => r is ParsedStochasticRule)
                .Select(x => x as ParsedStochasticRule)
                .GroupBy(x => x.targetSymbol)
                .Select(group =>
                {
                    var probabilityDeviation = Mathf.Abs(group.Sum(x => x.probability) - 1);
                    if (probabilityDeviation > 1e-30)
                    {
                        throw new System.Exception($"Error: group for {(char)group.Key} has probability {probabilityDeviation} away from 1");
                    }
                    return new BasicRule(group);
                });


            return basicRules.Concat(stochasticRules);
        }
    }
}
