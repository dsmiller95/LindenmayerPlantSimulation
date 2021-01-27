using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.ExpressionCompiler
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
                .Select(x =>
                {
                    if(stringsToTokens.TryGetValue(x, out var operatorToken))
                    {
                        return new Token(operatorToken);
                    }
                    if(double.TryParse(x, out var doubleConst))
                    {
                        return new Token(doubleConst);
                    }
                    if (variables != null && variables.Contains(x))
                    {
                        return new Token(x);
                    }
                    throw new Exception($"Token {x} is neither a numeric value, a variable, nor a syntatical token");
                });
        }

        public static IEnumerable<string> MergeTwoCharacterSymbols(IEnumerable<string> input)
        {
            string previousElement = null;
            foreach (var element in input)
            {
                if (string.IsNullOrEmpty(previousElement))
                {
                    previousElement = element;
                    continue;
                }
                if((previousElement == "<" || previousElement == ">") && element == "=")
                {
                    yield return previousElement + element;
                    previousElement = null;
                }else
                {
                    yield return previousElement;
                    previousElement = element;
                }
            }
            if (!string.IsNullOrEmpty(previousElement))
            {
                yield return previousElement;
            }
        }

        public static IEnumerable<string> SeperateToSymbols(string tokenString)
        {
            var delimiters = new[] { '(', '+', '-', '*', '/', '^', '>', '<', '=', ')' };
            var buffer = string.Empty;
            tokenString = tokenString.Replace(" ", "");
            foreach (var c in tokenString)
            {
                if (delimiters.Contains(c))
                {
                    if (buffer.Length > 0)
                    {
                        yield return buffer;
                    }
                    yield return "" + c;
                    buffer = string.Empty;
                }
                else
                {
                    buffer += c;
                }
            }
        }
    }
}
