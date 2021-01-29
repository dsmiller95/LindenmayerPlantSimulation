using Dman.LSystem.ExpressionCompiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public SingleSymbolMatcher[] targetSymbols;
        public SymbolReplacementExpressionMatcher[] replacementSymbols;

        public string TargetSymbolString()
        {
            return targetSymbols.Aggregate(new StringBuilder(), (agg, curr) => agg.Append(curr.ToString())).ToString();
        }
        public string ReplacementSymbolString()
        {
            return replacementSymbols.Aggregate(new StringBuilder(), (agg, curr) => agg.Append(curr.ToString())).ToString();
        }

        public string[] TargetSymbolParameterNames()
        {
            var result = new List<string>();
            foreach (var targetSymbol in targetSymbols)
            {
                if (targetSymbol?.namedParameters.Length >= 0)
                {
                    result.AddRange(targetSymbol.namedParameters);
                }
            }
            return result.ToArray();
        }

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
            var symbolMatchPattern = @"(\w(?:\((?:\w+, )*\w+\))?)+";

            var ruleMatch = Regex.Match(ruleString.Trim(), @$"(?:\(P(?<probability>[.0-9]+)\))?\s*(?<targetSymbols>{symbolMatchPattern})\s*->\s*(?<replacement>.+)");
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

            rule.targetSymbols = ParseSymbolMatcher(ruleMatch.Groups["targetSymbols"].Value);
            var replacementSymbolString = ruleMatch.Groups["replacement"].Value;
            rule.replacementSymbols = ParseAllSymbolExpressions(replacementSymbolString, rule.TargetSymbolParameterNames()).ToArray();
            return rule;
        }

        private static SingleSymbolMatcher[] ParseSymbolMatcher(string symbolSeries)
        {
            var individualSymbolTargets = Regex.Matches(symbolSeries, @"(?<symbol>\w)(?:\((?<params>(?:\w+, )*\w+)\))?");

            var targetSymbols = new List<SingleSymbolMatcher>();
            for (int i = 0; i < individualSymbolTargets.Count; i++)
            {
                var match = individualSymbolTargets[i];
                var symbol = match.Groups["symbol"].Value[0];
                var namedParameters = match.Groups["params"];
                var namedParamList = new List<string>();
                if (namedParameters.Success)
                {
                    var individualParamMatches = Regex.Matches(namedParameters.Value, @"(?<parameter>\w+),?\s*");
                    for (int j = 0; j < individualParamMatches.Count; j++)
                    {
                        namedParamList.Add(individualParamMatches[j].Groups["parameter"].Value);
                    }
                }
                targetSymbols.Add(new SingleSymbolMatcher(symbol, namedParamList));
            }

            return targetSymbols.ToArray();
        }

        private static IEnumerable<SymbolReplacementExpressionMatcher> ParseAllSymbolExpressions(string allsymbols, string[] validParameters)
        {
            var charEnumerator = allsymbols.GetEnumerator();
            charEnumerator.MoveNext();
            bool hasNextMatch;
            do
            {
                var nextMatch = ParseOutSymbolExpression(charEnumerator, validParameters);
                hasNextMatch = nextMatch.Item2;
                yield return nextMatch.Item1;
            } while (hasNextMatch);
        }

        /// <summary>
        /// parse out one symbol expression matcher. leave the enumerator on the character directly following the matched symbol.
        ///     return the next symbol, and whether or not there is another symbol
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="validParameters"></param>
        /// <returns></returns>
        private static (SymbolReplacementExpressionMatcher, bool) ParseOutSymbolExpression(IEnumerator<char> symbols, string[] validParameters)
        {
            var nextSymbol = symbols.Current;
            if(nextSymbol == '(' || nextSymbol == ')')
            {
                throw new SyntaxException("cannot use parentheses as a symbol");
            }
            if (!symbols.MoveNext())
            {
                return (new SymbolReplacementExpressionMatcher(nextSymbol), false);
            }
            if (symbols.Current != '(')
            {
                return (new SymbolReplacementExpressionMatcher(nextSymbol), true);
            }
            var delegates = new List<System.Delegate>();
            while(symbols.Current != ')')
            {
                symbols.MoveNext();
                var indentationDepth = 0;
                var expressionString = new StringBuilder();
                while (symbols.Current != ',')
                {
                    switch (symbols.Current)
                    {
                        case '(':
                            indentationDepth++;
                            break;
                        case ')':
                            indentationDepth--;
                            break;
                        default:
                            break;
                    }
                    if(indentationDepth < 0)
                    {
                        break;
                    }
                    expressionString.Append(symbols.Current);
                    symbols.MoveNext();
                }
                delegates.Add(ExpressionCompiler.ExpressionCompiler.CompileExpressionToDelegateWithParameters(
                    "(" + expressionString.ToString() + ")",
                    validParameters));
            }
            // reset to next char to stay consistent
            

            return (new SymbolReplacementExpressionMatcher(nextSymbol, delegates), symbols.MoveNext());
        }

        public static IEnumerable<IRule<double>> CompileRules(IEnumerable<string> ruleStrings)
        {
            var parsedRules = ruleStrings.Select(x => ParseToRule(x)).ToArray();
            var basicRules = parsedRules.Where(r => !(r is ParsedStochasticRule))
                .Select(x => new BasicRule(x)).ToList();

            var stochasticRules = parsedRules.Where(r => r is ParsedStochasticRule)
                .Select(x => x as ParsedStochasticRule)
                .GroupBy(x => x.targetSymbols, new ArrayElementEqualityComparer<SingleSymbolMatcher>()) // TODO: be sure that the array values are being compared here
                .Select(group =>
                {
                    var probabilityDeviation = Mathf.Abs(group.Sum(x => x.probability) - 1);
                    if (probabilityDeviation > 1e-30)
                    {
                        throw new System.Exception($"Error: group for {group.Key.Aggregate(new StringBuilder(), (agg, curr) => agg.Append(curr.ToString()))}"
                            + " has probability {probabilityDeviation} away from 1");
                    }
                    return new BasicRule(group);
                }).ToList();


            return basicRules.Concat(stochasticRules);
        }
    }
}
