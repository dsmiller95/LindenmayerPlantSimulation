namespace Dman.LSystem.SystemRuntime
{
    internal struct RuleOutcome
    {
        public double probability;
        public ReplacementSymbolGenerator[] replacementSymbols;

        public ushort ReplacementSymbolSize()
        {
            return (ushort)replacementSymbols.Length;
        }

        public SymbolString<float> GenerateReplacement(object[] matchedParameters)
        {
            var replacedSymbols = new int[replacementSymbols.Length];
            var replacedParams = new float[replacementSymbols.Length][];
            for (int symbolIndex = 0; symbolIndex < replacementSymbols.Length; symbolIndex++)
            {
                var replacementExpression = replacementSymbols[symbolIndex];

                replacedSymbols[symbolIndex] = replacementExpression.targetSymbol;
                replacedParams[symbolIndex] = replacementExpression.EvaluateNewParameters(matchedParameters);
            }

            return new SymbolString<float>(replacedSymbols, replacedParams);
        }
    }
}
