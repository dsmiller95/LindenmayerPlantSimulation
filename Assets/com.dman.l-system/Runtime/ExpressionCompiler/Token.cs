using System;
using System.Collections.Generic;

namespace Dman.LSystem.ExpressionCompiler
{
    [Flags]
    public enum TokenType
    {
        NONE = 0,
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

        public CompilerContext context;

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

        public Token(double constantValue, CompilerContext context)
        {
            token = TokenType.CONSTANT;
            value = constantValue;

            name = default;

            this.context = context;
        }

        public Token(TokenType type, CompilerContext context)
        {
            token = type;

            value = default;
            name = default;

            this.context = context;
        }

        public Token(string variableName, CompilerContext context)
        {
            token = TokenType.VARIABLE;
            name = variableName;

            value = default;

            this.context = context;
        }

        public override string ToString()
        {
            string symbolText;
            if (token == TokenType.VARIABLE)
            {
                symbolText = $"var \"{name}\"";
            }
            else if (token == TokenType.CONSTANT)
            {
                symbolText = $"const {value:F2}";
            }
            else
            {
                symbolText = Enum.GetName(typeof(TokenType), token);
            }

            return symbolText + $" : {context}";
        }
    }
}
