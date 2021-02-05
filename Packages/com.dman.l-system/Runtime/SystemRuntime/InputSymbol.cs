using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemRuntime
{
    /// <summary>
    /// represents a single target matcher. Used to match input symbols and input parameters
    /// </summary>
    internal class InputSymbol : System.IEquatable<InputSymbol>
    {
        public int targetSymbol;
        public string[] namedParameters;

        public InputSymbol(int targetSymbol, IEnumerable<string> namedParams)
        {
            this.targetSymbol = targetSymbol;
            namedParameters = namedParams.ToArray();
        }

        public override string ToString()
        {
            string result = ((char)targetSymbol) + "";
            if (namedParameters.Length > 0)
            {
                result += $"({namedParameters.Aggregate((agg, curr) => agg + ", " + curr)})";
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InputSymbol typed))
            {
                return false;
            }
            return Equals(typed);
        }

        public override int GetHashCode()
        {
            var hash = targetSymbol.GetHashCode();
            foreach (var parameter in namedParameters)
            {
                hash ^= parameter.GetHashCode();
            }
            return hash;
        }

        public bool Equals(InputSymbol other)
        {
            if (targetSymbol != other.targetSymbol || namedParameters.Length != other.namedParameters.Length)
            {
                return false;
            }
            for (int i = 0; i < namedParameters.Length; i++)
            {
                if (namedParameters[i] != other.namedParameters[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
