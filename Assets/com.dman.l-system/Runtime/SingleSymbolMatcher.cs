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
            int currentIndexInStream = -1;
            AttemptMoveNext(charEnumerator, ref currentIndexInStream);
            bool hasNextMatch;
            do
            {
                var nextMatch = ParseOutSymbolExpression(charEnumerator, validParameters, ref currentIndexInStream);
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
        /// <param name="currentIndexInStream"></param>
        /// <returns></returns>
        private static (SymbolReplacementExpressionMatcher, bool) ParseOutSymbolExpression(
            CharEnumerator symbols,
            string[] validParameters,
            ref int currentIndexInStream)
        {
            var nextSymbol = symbols.Current;
            if (nextSymbol == '(' || nextSymbol == ')')
            {
                throw new SyntaxException("Cannot use parentheses as a symbol", currentIndexInStream);
            }
            currentIndexInStream++;
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
                AttemptMoveNext(symbols, ref currentIndexInStream);
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
                    AttemptMoveNext(symbols, ref currentIndexInStream);
                }
                var expressionToParse = expressionString.ToString();
                try
                {
                    delegates.Add(ExpressionCompiler.ExpressionCompiler.CompileExpressionToDelegateWithParameters(
                        "(" + expressionToParse + ")",
                        validParameters));
                }
                catch (SyntaxException e)
                {
                    e.RecontextualizeIndex(currentIndexInStream - expressionToParse.Length - 1);
                    throw e;
                }
            }
            // reset to next char to stay consistent


            currentIndexInStream++;
            return (new SymbolReplacementExpressionMatcher(nextSymbol, delegates), symbols.MoveNext());
        }

        /// <summary>
        /// used when the next character must exist, and if it doesn't throw a parsing exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerator"></param>
        /// <param name="currentIndexInStream"></param>
        private static void AttemptMoveNext<T>(IEnumerator<T> enumerator, ref int currentIndexInStream)
        {
            currentIndexInStream++;
            if (!enumerator.MoveNext())
            {
                throw new SyntaxException("Unexpected end of input. Are you missing a parentheses?", currentIndexInStream);
            }
        }
    }

}
