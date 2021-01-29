using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem
{
    public interface ISymbolMatcher
    {
    }

    public class SingleSymbolMatcher : ISymbolMatcher, System.IEquatable<SingleSymbolMatcher>
    {
        public int targetSymbol;
        public string[] namedParameters;

        public SingleSymbolMatcher(int targetSymbol, IEnumerable<string> namedParams)
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
            if (!(obj is SingleSymbolMatcher typed))
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

        public bool Equals(SingleSymbolMatcher other)
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

    public class SymbolReplacementExpressionMatcher: ISymbolMatcher
    {
        public int targetSymbol;
        public Delegate[] evaluators;
        public SymbolReplacementExpressionMatcher(int targetSymbol)
        {
            this.targetSymbol = targetSymbol;
            evaluators = new Delegate[0];
        }
        public SymbolReplacementExpressionMatcher(int targetSymbol, IEnumerable<Delegate> evaluatorExpressions)
        {
            this.targetSymbol = targetSymbol;
            evaluators = evaluatorExpressions.ToArray();
        }

        public double[] EvaluateNewParameters(double[] matchedParameters)
        {
            return evaluators.Select(x => (double)x.DynamicInvoke(matchedParameters)).ToArray();
        }

        public override string ToString()
        {
            string result = ((char)targetSymbol) + "";
            if (evaluators.Length > 0)
            {
                result += @$"({evaluators
                    .Select(x => x.ToString())
                    .Aggregate((agg, curr) => agg + ", " + curr)})";
            }
            return result;
        }
    }
}
