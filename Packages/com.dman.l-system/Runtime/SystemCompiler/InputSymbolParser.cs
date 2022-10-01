using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dman.LSystem.SystemRuntime
{
    internal static class InputSymbolParser
    {
        /// <summary>
        /// parses strings in the format of "B(x, y)E(x)B" . extracts the symbols, and the names of the parameters in the inputs
        /// </summary>
        /// <remarks>
        /// Will trim any whitespace out of the string, around and between other symbols
        /// </remarks>
        /// <param name="symbolSeries"></param>
        /// <returns></returns>
        public static InputSymbol[] ParseInputSymbols(string symbolSeries, Func<char, int> symbolRemapper)
        {
            var individualSymbolTargets = Regex.Matches(symbolSeries, @"(?<symbol>[^:\s])(?:\(\s*(?<params>(?:\w+\s*,\s*)*\w+)\s*\))?");

            var targetSymbols = new List<InputSymbol>();
            for (int i = 0; i < individualSymbolTargets.Count; i++)
            {
                var match = individualSymbolTargets[i];
                int symbol;
                try
                {
                    symbol = symbolRemapper(match.Groups["symbol"].Value[0]);
                }
                catch (LSystemRuntimeException runtimeException)
                {
                    throw runtimeException.AddContext(symbolSeries);
                    //throw runtimeException;
                }
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
                targetSymbols.Add(new InputSymbol(symbol, namedParamList));
            }

            return targetSymbols.ToArray();
        }
    }

}
