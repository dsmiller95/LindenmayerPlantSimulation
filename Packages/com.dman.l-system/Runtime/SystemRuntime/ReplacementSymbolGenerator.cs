using System;
using System.Collections.Generic;
using System.Linq;
namespace Dman.LSystem.SystemRuntime
{
    internal class ReplacementSymbolGenerator
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

        public float[] EvaluateNewParameters(object[] matchedParameters)
        {
            // TODO: make the dynamic lambda operate on and return floats instead of doubles?
            return evaluators.Select(x => (float)x.DynamicInvoke(matchedParameters)).ToArray();
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
