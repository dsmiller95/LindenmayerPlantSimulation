namespace Dman.LSystem.SystemRuntime
{
    internal class SymbolSeriesMatcher
    {
        public InputSymbol[] targetSymbolSeries;

        public static SymbolSeriesMatcher Parse(string symbolString)
        {
            return new SymbolSeriesMatcher
            {
                targetSymbolSeries = InputSymbolParser.ParseInputSymbols(symbolString)
            };
        }
    }
}
