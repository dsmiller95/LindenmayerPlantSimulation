using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.ExpressionCompiler
{
    public enum TokenType
    {
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        EXPONENT,
        GREATER_THAN,
        LESS_THAN,
        GREATER_THAN_OR_EQ,
        LESS_THAN_OR_EQ,
        LEFT_PAREN,
        RIGHT_PAREN,
        CONSTANT,
        VARIABLE
    }

    public struct Token
    {
        public TokenType token;
        /// <summary>
        /// only set if token is CONSTANT
        /// </summary>
        public float value;
        /// <summary>
        /// only set if token is VARIABLE
        /// </summary>
        public string name;

        public Token(float constantValue)
        {
            token = TokenType.CONSTANT;
            value = constantValue;

            name = default;
        }

        public Token(TokenType type)
        {
            token = type;

            value = default;
            name = default;
        }

        public Token(string variableName)
        {
            token = TokenType.VARIABLE;
            name = variableName;

            value = default;
        }
    }
}
