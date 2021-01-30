using Dman.LSystem.SystemCompiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{
    public static class LSystemBuilder
    {
        public static LSystem<double> DoubleSystem(
           string axiomString,
           IEnumerable<string> rules,
           int seed,
           string[] globalParameters = null)
        {
            return new LSystem<double>(
                new SymbolString<double>(axiomString),
                ParsedRule.CompileRules(
                        rules,
                        globalParameters
                        ),
                seed,
                globalParameters?.Length ?? 0
                );
        }
    }

    public class LSystem<T>
    {
        public SymbolString<T> currentSymbols { get; private set; }
        /// <summary>
        /// structured data to store rules, in order of precidence, as follows:
        ///     first, order by size of the TargetSymbolSeries. Patterns which match more symbols always take precidence over patterns which match less symbols
        /// </summary>
        private IDictionary<int, IList<IRule<T>>> rulesByFirstTargetSymbol;
        private System.Random randomProvider;

        public int GlobalParameters { get; private set;}


        public LSystem(
            SymbolString<T> axiomString,
            IEnumerable<IRule<T>> rules,
            int seed,
            int expectedGlobalParameters = 0)
        {
            currentSymbols = axiomString;
            randomProvider = new System.Random(seed);
            this.GlobalParameters = expectedGlobalParameters;

            rulesByFirstTargetSymbol = new Dictionary<int, IList<IRule<T>>>();
            foreach (var rule in rules)
            {
                var targetSymbols = rule.TargetSymbolSeries;
                if (!rulesByFirstTargetSymbol.TryGetValue(targetSymbols[0], out var ruleList))
                {
                    rulesByFirstTargetSymbol[targetSymbols[0]] = ruleList = new List<IRule<T>>();
                }
                ruleList.Add(rule);
            }
            foreach (var symbol in rulesByFirstTargetSymbol.Keys.ToList())
            {
                rulesByFirstTargetSymbol[symbol] = rulesByFirstTargetSymbol[symbol]
                    .OrderByDescending(x => x.TargetSymbolSeries.Length)
                    .ToList();
            }
        }

        public void RestartSystem(string axiomString, int seed)
        {
            currentSymbols = new SymbolString<T>(axiomString);
            randomProvider = new System.Random(seed);
        }

        public void StepSystem(T[] globalParameters = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L system step");
            var globalParamSize = globalParameters?.Length ?? 0;
            if(globalParamSize != GlobalParameters)
            {
                throw new Exception($"Incomplete parameters provided. Expected {GlobalParameters} parameters but got {globalParamSize}");
            }

            var resultString = GenerateNextSymbols(globalParameters).ToList();
            currentSymbols = SymbolString<T>.ConcatAll(resultString);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private IEnumerable<SymbolString<T>> GenerateNextSymbols(T[] globalParameters)
        {
            for (int symbolIndex = 0; symbolIndex < currentSymbols.symbols.Length;)
            {
                var symbol = currentSymbols.symbols[symbolIndex];
                var parameters = currentSymbols.parameters[symbolIndex];
                var ruleApplied = false;
                if (rulesByFirstTargetSymbol.TryGetValue(symbol, out var ruleList) && ruleList != null && ruleList.Count > 0)
                {
                    foreach (var rule in ruleList)
                    {
                        var symbolMatch = rule.TargetSymbolSeries;
                        if (!MatchesSymbolStringAfterFirst(currentSymbols.symbols, symbolMatch, symbolIndex))
                        {
                            continue;
                        }
                        var result = rule.ApplyRule(
                            new ArraySegment<T[]>(currentSymbols.parameters, symbolIndex, symbolMatch.Length), 
                            randomProvider,
                            globalParameters);// todo
                        if (result != null)
                        {
                            yield return result;
                            symbolIndex += symbolMatch.Length;
                            ruleApplied = true;
                            break;
                        }
                    }
                }
                if (!ruleApplied)
                {
                    // if none of the rules match, which could happen if all of the matches for this char require additional subsequent characters
                    // or if there are no rules
                    yield return SymbolString<T>.FromSingle(symbol, parameters);
                    symbolIndex++;
                }
            }
        }

        private bool MatchesSymbolStringAfterFirst(
            int[] symbols,
            int[] targetSeries,
            int offset)
        {
            if (targetSeries.Length == 1)
            {
                return true;
            }
            for (int i = 1; i < targetSeries.Length; i++)
            {
                if (i + offset >= symbols.Length)
                {
                    return false;
                }
                if (symbols[i + offset] != targetSeries[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
