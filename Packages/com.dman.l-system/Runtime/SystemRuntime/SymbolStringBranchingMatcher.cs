namespace Dman.LSystem.SystemRuntime
{
    internal class SymbolStringBranchingMatcher
    {
        public static int defaultBranchOpenSymbol = '[';
        public static int defaultBranchCloseSymbol = ']';

        public int branchOpenSymbol;
        public int branchCloseSymbol;

        private ISymbolString symbolStringTarget;
        private int[] symbolJumpIndexes;

        public SymbolStringBranchingMatcher() : this(defaultBranchOpenSymbol, defaultBranchCloseSymbol) { }
        public SymbolStringBranchingMatcher(int open, int close)
        {
            branchOpenSymbol = open;
            branchCloseSymbol = close;
        }

        public void SetTargetSymbolString(ISymbolString symbols)
        {
            this.symbolStringTarget = symbols;
        }

        public bool MatchesForward(int index, SymbolSeriesMatcher seriesMatch)
        {
            return false;
        }
        public bool MatchesBackwards(int index, SymbolSeriesMatcher seriesMatch)
        {
            return false;
        }
    }
}
