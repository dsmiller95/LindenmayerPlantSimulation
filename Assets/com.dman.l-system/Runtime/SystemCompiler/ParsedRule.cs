using Dman.LSystem.SystemRuntime;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem.SystemCompiler
{
    public class ParsedStochasticRule : ParsedRule
    {
        public float probability;
    }

    public class ParsedRule
    {
        public InputSymbol[] targetSymbols;
        public ReplacementSymbolGenerator[] replacementSymbols;

        public System.Delegate conditionalMatch;
        public string conditionalStringDescription;

        public string TargetSymbolString()
        {
            return targetSymbols.Aggregate(new StringBuilder(), (agg, curr) => agg.Append(curr.ToString())).ToString();
        }
        public string ReplacementSymbolString()
        {
            return replacementSymbols.Aggregate(new StringBuilder(), (agg, curr) => agg.Append(curr.ToString())).ToString();
        }

        public override string ToString()
        {
            return $"{TargetSymbolString()} -> {ReplacementSymbolString()}";
        }

        public IEnumerable<string> TargetSymbolParameterNames()
        {
            var result = new List<string>();
            foreach (var targetSymbol in targetSymbols)
            {
                if (targetSymbol?.namedParameters.Length >= 0)
                {
                    result.AddRange(targetSymbol.namedParameters);
                }
            }
            return result;
        }

        private void SetConditional(string conditionalExpression, string[] validParameters)
        {
            try
            {
                var compiledResult = ExpressionCompiler.CompileExpressionToDelegateAndDescriptionWithParameters(
                    $"({conditionalExpression})",
                    validParameters);
                conditionalMatch = compiledResult.Item1;
                conditionalStringDescription = compiledResult.Item2;
            }
            catch (SyntaxException e)
            {
                e.RecontextualizeIndex(-1);
                throw;
            }
        }

        /// <summary>
        /// builds a rule based on the string definition, of format:
        ///   "A -> BACCB"
        ///   first char is always the target character
        ///   "->" delimits between target char and replacement string
        ///   everything after "->" is the replacement string
        /// the global parameters will come first in all dynamic handles generated
        /// </summary>
        /// <param name="ruleDef"></param>
        public static ParsedRule ParseToRule(string ruleString, string[] globalParameters = null)
        {
            var symbolMatchPattern = @"(\w(?:\((?:\w+, )*\w+\))?)+";

            var ruleMatch = Regex.Match(ruleString.Trim(), @$"(?:\(P(?<probability>[.0-9]+)\))?\s*(?<targetSymbols>{symbolMatchPattern})\s*(?::(?<conditional>.*)\s*)?->\s*(?<replacement>.+)");
            ParsedRule rule;
            if (ruleMatch.Groups["probability"].Success)
            {
                rule = new ParsedStochasticRule
                {
                    probability = float.Parse(ruleMatch.Groups["probability"].Value)
                };
            }
            else
            {
                rule = new ParsedRule();
            }

            rule.targetSymbols = InputSymbolParser.ParseInputSymbols(ruleMatch.Groups["targetSymbols"].Value);
            var availableParameters = rule.TargetSymbolParameterNames();
            if (globalParameters != null)
            {
                availableParameters = globalParameters.Concat(availableParameters);
            }
            var parameterArray = availableParameters.ToArray();

            var replacementSymbolMatch = ruleMatch.Groups["replacement"];

            if (!replacementSymbolMatch.Success)
            {
                throw new SyntaxException(
                    "Rule must follow pattern: <Target symbols> -> <replacement symbols>",
                    0,
                    ruleString.Length,
                    ruleString);
            }

            try
            {
                rule.replacementSymbols = ReplacementSymbolGeneratorParser.ParseReplacementSymbolGenerators(
                    replacementSymbolMatch.Value,
                    parameterArray)
                    .ToArray();
            }
            catch (SyntaxException e)
            {
                e.RecontextualizeIndex(replacementSymbolMatch.Index, ruleString);
                throw;
            }

            var conditionalMatch = ruleMatch.Groups["conditional"];
            if (conditionalMatch.Success)
            {
                try
                {
                    rule.SetConditional(conditionalMatch.Value, parameterArray);
                }
                catch (SyntaxException e)
                {
                    e.RecontextualizeIndex(conditionalMatch.Index, ruleString);
                    throw;
                }
            }

            return rule;
        }


        public static IEnumerable<IRule<double>> CompileRules(IEnumerable<string> ruleStrings, string[] globalParameters = null)
        {
            var parsedRules = ruleStrings
                .Select(x => ParseToRule(x, globalParameters))
                .Where(x => x != null)
                .ToArray();
            var basicRules = parsedRules.Where(r => !(r is ParsedStochasticRule))
                .Select(x => new BasicRule(x)).ToList();

            IEqualityComparer<ParsedRule> ruleComparer = new ParsedRuleEqualityComparer();
#if UNITY_EDITOR
            for (int i = 0; i < parsedRules.Length; i++)
            {
                if (parsedRules[i] is ParsedStochasticRule)
                {
                    continue;
                }
                for (int j = i + 1; j < parsedRules.Length; j++)
                {
                    if (parsedRules[j] is ParsedStochasticRule)
                    {
                        continue;
                    }
                    if (ruleComparer.Equals(parsedRules[i], parsedRules[j]))
                    {
                        throw new System.Exception($"Cannot have two non-stochastic rules matching the same symbols. matching rules: {parsedRules[i]} {parsedRules[j]}");
                    }
                }
            }
#endif

            var stochasticRules = parsedRules.Where(r => r is ParsedStochasticRule)
                .Select(x => x as ParsedStochasticRule)
                //.GroupBy(x => x.targetSymbols, new ArrayElementEqualityComparer<SingleSymbolMatcher>())
                .GroupBy(x => x, ruleComparer)
                .Select(group =>
                {
                    var probabilityDeviation = Mathf.Abs(group.Sum(x => x.probability) - 1);
                    if (probabilityDeviation > 1e-30)
                    {
                        throw new System.Exception($"Error: group for {group.Key.targetSymbols.Aggregate(new StringBuilder(), (agg, curr) => agg.Append(curr.ToString()))}"
                            + $" has probability {probabilityDeviation} away from 1");
                    }
                    return new BasicRule(group);
                }).ToList();


            return basicRules.Concat(stochasticRules);
        }
    }
}
