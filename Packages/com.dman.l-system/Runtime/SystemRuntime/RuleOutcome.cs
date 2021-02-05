namespace Dman.LSystem.SystemRuntime
{
    internal struct RuleOutcome
    {
        public double probability;
        public ReplacementSymbolGenerator[] replacementSymbols;

        public SymbolString<double> GenerateReplacement(object[] matchedParameters)
        {
            var replacedSymbols = new int[replacementSymbols.Length];
            var replacedParams = new double[replacementSymbols.Length][];
            for (int symbolIndex = 0; symbolIndex < replacementSymbols.Length; symbolIndex++)
            {
                var replacementExpression = replacementSymbols[symbolIndex];

                replacedSymbols[symbolIndex] = replacementExpression.targetSymbol;
                replacedParams[symbolIndex] = replacementExpression.EvaluateNewParameters(matchedParameters);
            }

            return new SymbolString<double>(replacedSymbols, replacedParams);
        }
    }
}
