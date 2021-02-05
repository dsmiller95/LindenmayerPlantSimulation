using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemCompiler
{
    internal class Tokenizer
    {
        private static Dictionary<string, TokenType> stringsToTokens = new Dictionary<string, TokenType>{
                { "*", TokenType.MULTIPLY},
                { "/", TokenType.DIVIDE},
                { "+", TokenType.ADD},
                { "-", TokenType.SUBTRACT},
                { "%", TokenType.REMAINDER},
                { "^", TokenType.EXPONENT},

                { ">", TokenType.GREATER_THAN},
                { "<", TokenType.LESS_THAN},
                { ">=", TokenType.GREATER_THAN_OR_EQ},
                { "<=", TokenType.LESS_THAN_OR_EQ},
                { "==", TokenType.EQUAL},
                { "!=", TokenType.NOT_EQUAL},

                { "&&", TokenType.BOOLEAN_AND},
                { "||", TokenType.BOOLEAN_OR},
                { "!", TokenType.BOOLEAN_NOT},

                { "(", TokenType.LEFT_PAREN},
                { ")", TokenType.RIGHT_PAREN}
                };
        private static HashSet<string> twoCharacterTokens = new HashSet<string>
        {
            ">=", "<=", "==", "!=",
            "&&", "||"
        };

        private static HashSet<char> delimiters = new HashSet<char>
        {
            '(', ')',
            '*', '/', '%',
            '^',
            '+', '-',
            '>', '<', '=', '!',
            '&', '|'
        };

        public static IEnumerable<Token> Tokenize(string tokenString, string[] variables = null)
        {
            return MergeTwoCharacterSymbols(SeperateToSymbols(tokenString), twoCharacterTokens)
                .Select(seg =>
                {
                    if (stringsToTokens.TryGetValue(seg.value, out var operatorToken))
                    {
                        return new Token(operatorToken, seg.context);
                    }
                    if (double.TryParse(seg.value, out var doubleConst))
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

        public static IEnumerable<StringSegment> MergeTwoCharacterSymbols(
            IEnumerable<StringSegment> input,
            HashSet<string> twoCharSymbols)
        {
            StringSegment previousElement = default;
            foreach (var element in input)
            {
                if (string.IsNullOrEmpty(previousElement.value))
                {
                    previousElement = element;
                    continue;
                }
                var twoCharSymbol = StringSegment.MergeConsecutive(previousElement, element);
                if (twoCharSymbols.Contains(twoCharSymbol.value))
                {
                    yield return twoCharSymbol;
                    previousElement = default;
                }
                else
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
            var buffer = string.Empty;
            var skippedSpacesInBuffer = 0;
            int indexInString = -1;
            //tokenString = tokenString.Replace(" ", "");
            foreach (var c in tokenString)
            {
                indexInString++;
                if (c == ' ')
                {
                    if (buffer != string.Empty) // don't track spaces before the buffer starts getting filled
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
