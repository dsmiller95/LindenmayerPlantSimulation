using Dman.LSystem.SystemRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dman.LSystem.SystemCompiler
{
    public class ParsedStochasticRule : ParsedRule
    {
        public double probability;
    }

    public class ParsedRule
    {
        public short ruleGroupIndex;
        public InputSymbol coreSymbol;
        public InputSymbol[] backwardsMatch;
        public InputSymbol[] forwardsMatch;
        public ReplacementSymbolGenerator[] replacementSymbols;

        public DynamicExpressionData conditionalMatch;
        public string conditionalStringDescription;

        public string TargetSymbolString()
        {
            var builder = new StringBuilder();
            if (backwardsMatch?.Length > 0)
            {
                builder.Append(backwardsMatch.JoinText(""));
                builder.Append(" < ");
            }
            builder.Append(coreSymbol);
            if (forwardsMatch?.Length > 0)
            {
                builder.Append(" > ");
                builder.Append(forwardsMatch.JoinText(""));
            }
            return builder.ToString();
        }
        public string ReplacementSymbolString()
        {
            return replacementSymbols.JoinText("");
        }

        public override string ToString()
        {
            return $"{TargetSymbolString()} -> {ReplacementSymbolString()}";
        }

        /// <summary>
        /// returns all parameter names in the match pattern, in the order they would have appeared in the rule string
        ///     including parameter names captured as part of a contextual match
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> TargetSymbolParameterNames()
        {
            var result = new List<string>();
            foreach (var targetSymbol in backwardsMatch.Append(coreSymbol).Concat(forwardsMatch))
            {
                if (targetSymbol.parameterLength >= 0)
                {
                    result.AddRange(targetSymbol.parameterNames);
                }
            }
            return result;
        }

        public void SetConditional(string conditionalExpression, string[] validParameters)
        {
            try
            {
                conditionalMatch = ExpressionCompiler.CompileExpressionToDelegateWithParameters(
                    $"({conditionalExpression})",
                    validParameters);
                conditionalStringDescription = conditionalMatch.OgExpressionStringValue;
            }
            catch (SyntaxException e)
            {
                e.RecontextualizeIndex(-1);
                throw;
            }
        }

        public void ParseContextualMatches(Capture contextualMatchInput, Func<char, int> symbolRemapper)
        {
            var contextualMatchSection = contextualMatchInput.Value;
            var contextualMatch = Regex.Match(contextualMatchSection, @"(?:(?<prefix>[^<>]+)\s*<\s*)?(?<target>[^<>]+)(?:\s*>(?<suffix>[^<>]+))?");

            if (!contextualMatch.Success || !contextualMatch.Groups["target"].Success)
            {
                throw new SyntaxException("Must specify a single target symbol", contextualMatchInput);
            }
            var targetSymbols = InputSymbolParser.ParseInputSymbols(contextualMatch.Groups["target"].Value, symbolRemapper);
            if (targetSymbols.Length != 1)
            {
                throw new SyntaxException("Multi match target symbols are not supported. Convert this rule into mutliple context-sensitive rules.", contextualMatch.Groups["target"]);
            }
            coreSymbol = targetSymbols[0];
            if (contextualMatch.Groups["prefix"].Success)
            {
                backwardsMatch = InputSymbolParser.ParseInputSymbols(contextualMatch.Groups["prefix"].Value, symbolRemapper);
            }
            else
            {
                backwardsMatch = new InputSymbol[0];
            }
            if (contextualMatch.Groups["suffix"].Success)
            {
                forwardsMatch = InputSymbolParser.ParseInputSymbols(contextualMatch.Groups["suffix"].Value, symbolRemapper);
            }
            else
            {
                forwardsMatch = new InputSymbol[0];
            }
        }
    }
}
