using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.SystemRuntime
{
    /// <summary>
    /// represents a single target matcher. Used to match input symbols and input parameters
    /// </summary>
    public struct InputSymbol : System.IEquatable<InputSymbol>
    {
        public int targetSymbol { get; private set; }
        public int parameterLength { get; private set; }
        public IEnumerable<string> parameterNames => namedParameters;
        [NativeSetClassTypeToNullOnSchedule]
        private string[] namedParameters;

        public InputSymbol(int targetSymbol, IEnumerable<string> namedParams)
        {
            this.targetSymbol = targetSymbol;
            namedParameters = namedParams.ToArray();
            parameterLength = namedParameters.Length;
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
            hash ^= parameterLength << 32;
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
