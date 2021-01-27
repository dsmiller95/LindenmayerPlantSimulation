using System;
using System.Collections.Generic;

namespace Dman.LSystem.ExpressionCompiler
{
    [Flags]
    public enum TokenType
    {
        MULTIPLY = 1 << 0,
        DIVIDE = 1 << 1,
        ADD = 1 << 2,
        SUBTRACT = 1 << 3,
        EXPONENT = 1 << 4,
        GREATER_THAN = 1 << 5,
        LESS_THAN = 1 << 6,
        GREATER_THAN_OR_EQ = 1 << 7,
        LESS_THAN_OR_EQ = 1 << 8,


        LEFT_PAREN = 1 << 9,
        RIGHT_PAREN = 1 << 10,
        CONSTANT = 1 << 11,
        VARIABLE = 1 << 12
    }

    public struct Token
    {
        public TokenType token;
        /// <summary>
        /// only set if token is CONSTANT
        /// </summary>
        public double value;
        /// <summary>
        /// only set if token is VARIABLE
        /// </summary>
        public string name;

        public static readonly Dictionary<TokenType, int> OPERATOR_PRECIDENCE = new Dictionary<TokenType, int>
        {
            {TokenType.MULTIPLY, 0 },
            {TokenType.DIVIDE, 0 },
            {TokenType.EXPONENT, 1 },
            {TokenType.ADD, 2 },
            {TokenType.SUBTRACT, 2 },
            {TokenType.GREATER_THAN, 3 },
            {TokenType.LESS_THAN, 3 },
            {TokenType.GREATER_THAN_OR_EQ, 3 },
            {TokenType.LESS_THAN_OR_EQ, 3 },
        };

        public Token(double constantValue)
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
