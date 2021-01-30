using System;
using System.Collections.Generic;
using System.Linq;
namespace Dman.LSystem.SystemRuntime
{
    public class ReplacementSymbolGenerator
    {
        public int targetSymbol;
        public Delegate[] evaluators;
        public ReplacementSymbolGenerator(int targetSymbol)
        {
            this.targetSymbol = targetSymbol;
            evaluators = new Delegate[0];
        }
        public ReplacementSymbolGenerator(int targetSymbol, IEnumerable<Delegate> evaluatorExpressions)
        {
            this.targetSymbol = targetSymbol;
            evaluators = evaluatorExpressions.ToArray();
        }

        public double[] EvaluateNewParameters(object[] matchedParameters)
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
