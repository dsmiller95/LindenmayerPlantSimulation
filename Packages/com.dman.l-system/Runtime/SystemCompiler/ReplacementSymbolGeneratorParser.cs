using Dman.LSystem.SystemCompiler;
using System;
using System.Collections.Generic;
using System.Text;
namespace Dman.LSystem.SystemRuntime
{
    internal static class ReplacementSymbolGeneratorParser
    {
        public static IEnumerable<ReplacementSymbolGenerator> ParseReplacementSymbolGenerators(string allsymbols, string[] validParameters)
        {
            if(allsymbols.Length <= 0)
            {
                yield break;
            }
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
        private static (ReplacementSymbolGenerator, bool) ParseOutSymbolExpression(
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
                return (new ReplacementSymbolGenerator(nextSymbol), false);
            }
            if (symbols.Current != '(')
            {
                return (new ReplacementSymbolGenerator(nextSymbol), true);
            }
            var delegates = new List<DynamicExpressionData>();
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
                    delegates.Add(ExpressionCompiler.CompileExpressionToDelegateWithParameters(
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
            return (new ReplacementSymbolGenerator(nextSymbol, delegates), symbols.MoveNext());
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
