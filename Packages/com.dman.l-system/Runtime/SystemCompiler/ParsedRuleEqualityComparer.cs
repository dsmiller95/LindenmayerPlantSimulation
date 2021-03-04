using Dman.LSystem.SystemRuntime;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler
{
    internal class ParsedRuleEqualityComparer : IEqualityComparer<ParsedRule>
    {
        public bool Equals(ParsedRule x, ParsedRule y)
        {
            if (!x.coreSymbol.Equals(y.coreSymbol))
            {
                return false;
            }
            if (!TargetSymbolsEqual(x.backwardsMatch, y.backwardsMatch))
            {
                return false;
            }
            if (!TargetSymbolsEqual(x.forwardsMatch, y.forwardsMatch))
            {
                return false;
            }
            if (x.conditionalStringDescription != y.conditionalStringDescription)
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
            // if both x and y are null, previous case catches that.
            if (x == null || y == null || x.Length != y.Length)
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
            for (int i = 0; i < obj.backwardsMatch.Length; i++)
            {
                var symbol = obj.backwardsMatch[i];
                hashCode ^= symbol.GetHashCode();
            }
            hashCode ^= obj.coreSymbol.GetHashCode();
            for (int i = 0; i < obj.forwardsMatch.Length; i++)
            {
                var symbol = obj.forwardsMatch[i];
                hashCode ^= symbol.GetHashCode();
            }
            if (obj.conditionalStringDescription != null)
            {
                hashCode ^= obj.conditionalStringDescription.GetHashCode();
            }
            return hashCode;
        }
    }
}
