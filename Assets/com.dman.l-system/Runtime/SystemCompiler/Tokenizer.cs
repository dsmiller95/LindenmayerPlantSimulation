using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemCompiler
{
    public class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(string tokenString, string[] variables = null)
        {
            var stringsToTokens = new Dictionary<string, TokenType>{
                { "+", TokenType.ADD},
                { "-", TokenType.SUBTRACT},
                { "*", TokenType.MULTIPLY},
                { "/", TokenType.DIVIDE},
                { "^", TokenType.EXPONENT},
                { ">", TokenType.GREATER_THAN},
                { ">=", TokenType.GREATER_THAN_OR_EQ},
                { "<", TokenType.LESS_THAN},
                { "<=", TokenType.LESS_THAN_OR_EQ},
                { "(", TokenType.LEFT_PAREN},
                { ")", TokenType.RIGHT_PAREN}
                };

            return MergeTwoCharacterSymbols(SeperateToSymbols(tokenString))
                .Select(seg =>
                {
                    if(stringsToTokens.TryGetValue(seg.value, out var operatorToken))
                    {
                        return new Token(operatorToken, seg.context);
                    }
                    if(double.TryParse(seg.value, out var doubleConst))
                    {
                        return new Token(doubleConst, seg.context);
                    }
                    if (variables != null && variables.Contains(seg.value))
                    {
                        return new Token(seg.value, seg.context);
                    }

                    throw seg.context.ExceptionHere($"Token \"{seg}\" is neither a numeric value, a variable, nor a syntatical token");
                });
        }

        public static IEnumerable<StringSegment> MergeTwoCharacterSymbols(IEnumerable<StringSegment> input)
        {
            StringSegment previousElement = default;
            foreach (var element in input)
            {
                if (string.IsNullOrEmpty(previousElement.value))
                {
                    previousElement = element;
                    continue;
                }
                if((previousElement.value == "<" || previousElement.value == ">") && element.value == "=")
                {
                    yield return StringSegment.MergeConsecutive(previousElement, element);
                    previousElement = default;
                }else
                {
                    yield return previousElement;
                    previousElement = element;
                }
            }
            if (!string.IsNullOrEmpty(previousElement.value))
            {
                yield return previousElement;
            }
        }

        public static IEnumerable<StringSegment> SeperateToSymbols(string tokenString)
        {
            var delimiters = new[] { '(', '+', '-', '*', '/', '^', '>', '<', '=', ')' };
            var buffer = string.Empty;
            var skippedSpacesInBuffer = 0;
            int indexInString = -1;
            //tokenString = tokenString.Replace(" ", "");
            foreach (var c in tokenString)
            {
                indexInString++;
                if (c == ' ')
                {
                    if(buffer != string.Empty) // don't track spaces before the buffer starts getting filled
                    {
                        skippedSpacesInBuffer++;
                    }
                    continue;
                }
                if (delimiters.Contains(c))
                {
                    if (buffer.Length > 0)
                    {
                        yield return new StringSegment(
                            buffer,
                            new CompilerContext(
                                indexInString - (buffer.Length + skippedSpacesInBuffer),
                                indexInString - skippedSpacesInBuffer));
                    }
                    yield return new StringSegment("" + c, new CompilerContext(indexInString, indexInString + 1));
                    buffer = string.Empty;
                    skippedSpacesInBuffer = 0;
                }
                else
                {
                    buffer += c;
                }
            }
        }
    }
}
