using Dman.LSystem.ExpressionCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class SymbolReplacementExpressionMatcher : ISymbolMatcher
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
        public static IEnumerable<SymbolReplacementExpressionMatcher> ParseAllSymbolExpressions(string allsymbols, string[] validParameters)
        {
            var charEnumerator = allsymbols.GetEnumerator();
            charEnumerator.MoveNext();
            bool hasNextMatch;
            do
            {
                var nextMatch = ParseOutSymbolExpression(charEnumerator, validParameters);
                hasNextMatch = nextMatch.Item2;
                yield return nextMatch.Item1;
            } while (hasNextMatch);
        }

        /// <summary>
        /// parse out one symbol expression matcher. leave the enumerator on the character directly following the matched symbol.
        ///     return the next symbol, and whether or not there is another symbol
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="validParameters"></param>
        /// <returns></returns>
        private static (SymbolReplacementExpressionMatcher, bool) ParseOutSymbolExpression(IEnumerator<char> symbols, string[] validParameters)
        {
            var nextSymbol = symbols.Current;
            if (nextSymbol == '(' || nextSymbol == ')')
            {
                throw new SyntaxException("cannot use parentheses as a symbol");
            }
            if (!symbols.MoveNext())
            {
                return (new SymbolReplacementExpressionMatcher(nextSymbol), false);
            }
            if (symbols.Current != '(')
            {
                return (new SymbolReplacementExpressionMatcher(nextSymbol), true);
            }
            var delegates = new List<System.Delegate>();
            while (symbols.Current != ')')
            {
                symbols.MoveNext();
                var indentationDepth = 0;
                var expressionString = new StringBuilder();
                while (symbols.Current != ',')
                {
                    switch (symbols.Current)
                    {
                        case '(':
                            indentationDepth++;
                            break;
                        case ')':
                            indentationDepth--;
                            break;
                        default:
                            break;
                    }
                    if (indentationDepth < 0)
                    {
                        break;
                    }
                    expressionString.Append(symbols.Current);
                    symbols.MoveNext();
                }
                delegates.Add(ExpressionCompiler.ExpressionCompiler.CompileExpressionToDelegateWithParameters(
                    "(" + expressionString.ToString() + ")",
                    validParameters));
            }
            // reset to next char to stay consistent


            return (new SymbolReplacementExpressionMatcher(nextSymbol, delegates), symbols.MoveNext());
        }
    }
}
