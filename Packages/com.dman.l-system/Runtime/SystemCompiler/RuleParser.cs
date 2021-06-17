using Dman.LSystem.SystemRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dman.LSystem.SystemCompiler
{
    internal static class RuleParser
    {

        /// <summary>
        /// builds a rule based on the string definition, of format:
        ///   "A -> BACCB"
        ///   first char is always the target character
        ///   "->" delimits between target char and replacement string
        ///   everything after "->" is the replacement string
        /// the global parameters will come first in all dynamic handles generated
        /// reserved symbols:
        ///     : | ->
        /// </summary>
        /// <param name="ruleDef"></param>
        public static ParsedRule ParseToRule(
            string ruleString,
            Func<char, int> symbolRemapper = null,
            short sourceFileIndex = 0,
            string[] globalParameters = null)
        {
            if (symbolRemapper == null)
            {
                symbolRemapper = x => x;
            }
            var centralDelimimiterMatch = Regex.Match(ruleString.Trim(), @"(?<matcher>.*)->\s*(?<replacement>.*)\s*");
            if (!centralDelimimiterMatch.Success || !centralDelimimiterMatch.Groups["matcher"].Success || !centralDelimimiterMatch.Groups["replacement"].Success)
            {
                throw new SyntaxException(
                    "Rule must follow pattern: <Target symbols> -> <replacement symbols>",
                    0,
                    ruleString.Length,
                    ruleString);
            }

            var matcherMatch = Regex.Match(centralDelimimiterMatch.Groups["matcher"].Value.Trim(), @"(?:P(?<probability>\([^:|]+\))\s*\|)?\s*(?<contextMatch>[^:|]+)\s*(?::(?<conditional>.*)\s*)?");

            if (!matcherMatch.Success)
            {
                throw new SyntaxException(
                    "Rule must follow pattern: <Target symbols> -> <replacement symbols>",
                    0,
                    ruleString.Length,
                    ruleString);
            }

            ParsedRule rule;
            var probabilityMatch = matcherMatch.Groups["probability"];
            if (probabilityMatch.Success)
            {
                var probabilityExpression = probabilityMatch.Value.Trim();
                try
                {
                    var compiledProbabilityExpression = ExpressionCompiler.CompileExpressionToDelegateWithParameters(probabilityExpression);
                    rule = new ParsedStochasticRule
                    {
                        probability = compiledProbabilityExpression.DynamicInvoke()
                    };
                }
                catch (SyntaxException e)
                {
                    e.RecontextualizeIndex(probabilityMatch.Index, ruleString);
                    throw;
                }
            }
            else
            {
                rule = new ParsedRule();
            }
            rule.ruleGroupIndex = sourceFileIndex;

            var contextMatch = matcherMatch.Groups["contextMatch"];
            try
            {
                rule.ParseContextualMatches(contextMatch, symbolRemapper);
            }
            catch (SyntaxException e)
            {
                e.RecontextualizeIndex(contextMatch.Index, ruleString);
                throw;
            }

            var availableParameters = rule.TargetSymbolParameterNames();
            if (globalParameters != null)
            {
                availableParameters = globalParameters.Concat(availableParameters);
            }
            var parameterArray = availableParameters.ToArray();

            var replacementSymbolMatch = centralDelimimiterMatch.Groups["replacement"];

            try
            {
                if (replacementSymbolMatch.Success)
                {
                    rule.replacementSymbols = ReplacementSymbolGeneratorParser.ParseReplacementSymbolGenerators(
                        replacementSymbolMatch.Value,
                        parameterArray,
                        symbolRemapper)
                        .ToArray();
                }
                else
                {
                    rule.replacementSymbols = new ReplacementSymbolGenerator[0];
                }
            }
            catch (SyntaxException e)
            {
                e.RecontextualizeIndex(replacementSymbolMatch.Index, ruleString);
                throw;
            }

            var conditionalMatch = matcherMatch.Groups["conditional"];
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

        //TODO: get rid of this function. should only compile from linked files
        public static IEnumerable<BasicRule> CompileRules(
            IEnumerable<string> ruleStrings,
            out SystemLevelRuleNativeData ruleNativeData,
            int branchOpenSymbol, int branchCloseSymbol,
            string[] globalParameters = null)
        {
            var parsedRules = ruleStrings
                .Select(x => ParseToRule(x, x => x, globalParameters: globalParameters))
                .Where(x => x != null)
                .ToArray();
            return CompileAndCheckParsedRules(parsedRules, out ruleNativeData, branchOpenSymbol, branchCloseSymbol);
        }

        public static IEnumerable<BasicRule> CompileAndCheckParsedRules(
            ParsedRule[] parsedRules,
            out SystemLevelRuleNativeData ruleNativeData,
            int branchOpenSymbol, int branchCloseSymbol)
        {
            var basicRules = parsedRules.Where(r => !(r is ParsedStochasticRule))
                .Select(x => new BasicRule(x, branchOpenSymbol, branchCloseSymbol)).ToList();

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
                        throw new LSystemRuntimeException($"Cannot have two non-stochastic rules matching the same symbols. matching rules: {parsedRules[i]} {parsedRules[j]}");
                    }
                }
            }
#endif

            var stochasticRules = parsedRules.Where(r => r is ParsedStochasticRule)
                .Select(x => x as ParsedStochasticRule)
                .GroupBy(x => x, ruleComparer)
                .Select(group =>
                {
                    var probabilityDeviation = System.Math.Abs(group.Sum(x => x.probability) - 1);
                    if (probabilityDeviation > 1e-5)
                    {
                        throw new LSystemRuntimeException($"Error: group for {group.Key.TargetSymbolString()}"
                            + $" has probability {probabilityDeviation} away from 1");
                    }
                    return new BasicRule(group, branchOpenSymbol, branchCloseSymbol);
                }).ToList();


            var allRules = basicRules.Concat(stochasticRules).ToArray();
            ruleNativeData = new SystemLevelRuleNativeData(allRules);
            var nativeWriter = new SymbolSeriesMatcherNativeDataWriter();

            foreach (var rule in allRules)
            {
                rule.WriteDataIntoMemory(ruleNativeData, nativeWriter);
            }

            return allRules;
        }
    }
}
