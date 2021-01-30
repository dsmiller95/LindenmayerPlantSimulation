using System.Collections.Generic;

namespace Dman.LSystem
{
    public class ParsedRuleEqualityComparer : IEqualityComparer<ParsedRule>
    {
        public bool Equals(ParsedRule x, ParsedRule y)
        {
            if(!TargetSymbolsEqual(x.targetSymbols, y.targetSymbols))
            {
                return false;
            }

            return true;
        }

        private bool TargetSymbolsEqual(SingleSymbolMatcher[] x, SingleSymbolMatcher[] y)
        {
            if (x == y)
            {
                return true;
            }
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (!x.Equals(y))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(ParsedRule obj)
        {
            int hashCode = 0;
            for (int i = 0; i < obj.targetSymbols.Length; i++)
            {
                var symbol = obj.targetSymbols[i];
                hashCode ^= symbol.GetHashCode();
            }
            return hashCode;
        }
    }
}
