using Dman.LSystem.SystemCompiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{
    public static class LSystemBuilder
    {
        public static LSystem<double> DoubleSystem(
           IEnumerable<string> rules,
           int seed,
           string[] globalParameters = null)
        {
            return new LSystem<double>(
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

        public int GlobalParameters { get; private set; }


        public LSystem(
            IEnumerable<IRule<T>> rules,
            int seed,
            int expectedGlobalParameters = 0)
        {
            randomProvider = new System.Random(seed);
            GlobalParameters = expectedGlobalParameters;

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

        public void RestartSystem(int seed)
        {
            randomProvider = new System.Random(seed);
        }

        public SymbolString<T> StepSystem(SymbolString<T> initialState, T[] globalParameters = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L system step");
            var globalParamSize = globalParameters?.Length ?? 0;
            if (globalParamSize != GlobalParameters)
            {
                throw new Exception($"Incomplete parameters provided. Expected {GlobalParameters} parameters but got {globalParamSize}");
            }

            var resultString = GenerateNextSymbols(initialState, globalParameters).ToList();
            var resultSymbols = SymbolString<T>.ConcatAll(resultString);
            UnityEngine.Profiling.Profiler.EndSample();
            return resultSymbols;
        }

        private IEnumerable<SymbolString<T>> GenerateNextSymbols(SymbolString<T> initialState, T[] globalParameters)
        {
            for (int symbolIndex = 0; symbolIndex < initialState.symbols.Length;)
            {
                var symbol = initialState.symbols[symbolIndex];
                var parameters = initialState.parameters[symbolIndex];
                var ruleApplied = false;
                if (rulesByFirstTargetSymbol.TryGetValue(symbol, out var ruleList) && ruleList != null && ruleList.Count > 0)
                {
                    foreach (var rule in ruleList)
                    {
                        var symbolMatch = rule.TargetSymbolSeries;
                        if (!MatchesSymbolStringAfterFirst(initialState.symbols, symbolMatch, symbolIndex))
                        {
                            continue;
                        }
                        var result = rule.ApplyRule(
                            new ArraySegment<T[]>(initialState.parameters, symbolIndex, symbolMatch.Length),
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
