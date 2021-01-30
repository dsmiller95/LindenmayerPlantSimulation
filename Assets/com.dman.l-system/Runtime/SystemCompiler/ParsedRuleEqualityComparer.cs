using Dman.LSystem.SystemRuntime;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler
{
    public class ParsedRuleEqualityComparer : IEqualityComparer<ParsedRule>
    {
        public bool Equals(ParsedRule x, ParsedRule y)
        {
            if(!TargetSymbolsEqual(x.targetSymbols, y.targetSymbols))
            {
                return false;
            }
            if(x.conditionalStringDescription != y.conditionalStringDescription)
            {
                return false;
            }

            return true;
        }

        private bool TargetSymbolsEqual(InputSymbol[] x, InputSymbol[] y)
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
                if (!x[i].Equals(y[i]))
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
            if(obj.conditionalStringDescription != null)
            {
                hashCode ^= obj.conditionalStringDescription.GetHashCode();
            }
            return hashCode;
        }
    }
}
