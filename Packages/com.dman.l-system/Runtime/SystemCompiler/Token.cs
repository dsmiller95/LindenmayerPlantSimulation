﻿using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler
{
    [Flags]
    public enum TokenType
    {
        NONE = 0,

        MULTIPLY = 1 << 0,
        DIVIDE = 1 << 1,
        ADD = 1 << 2,
        SUBTRACT = 1 << 3,
        REMAINDER = 1 << 4,
        EXPONENT = 1 << 5,

        GREATER_THAN = 1 << 6,
        LESS_THAN = 1 << 7,
        GREATER_THAN_OR_EQ = 1 << 8,
        LESS_THAN_OR_EQ = 1 << 9,
        EQUAL = 1 << 10,
        NOT_EQUAL = 1 << 11,

        BOOLEAN_AND = 1 << 12,
        BOOLEAN_OR = 1 << 13,
        BOOLEAN_NOT = 1 << 14,

        LEFT_PAREN = 1 << 15,
        RIGHT_PAREN = 1 << 16,
        CONSTANT = 1 << 17,
        VARIABLE = 1 << 18
    }

    internal struct Token
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

        public CompilerContext context;

        public static readonly Dictionary<TokenType, int> OPERATOR_PRECIDENCE = new Dictionary<TokenType, int>
        {
            {TokenType.MULTIPLY, 0 },
            {TokenType.DIVIDE, 0 },
            {TokenType.REMAINDER, 0 },
            {TokenType.EXPONENT, 1 },
            {TokenType.ADD, 2 },
            {TokenType.SUBTRACT, 2 },
            {TokenType.GREATER_THAN, 3 },
            {TokenType.LESS_THAN, 3 },
            {TokenType.GREATER_THAN_OR_EQ, 3 },
            {TokenType.LESS_THAN_OR_EQ, 3 },
            {TokenType.EQUAL, 4 },
            {TokenType.NOT_EQUAL, 4 },
            {TokenType.BOOLEAN_AND, 5 },
            {TokenType.BOOLEAN_OR, 6 },
        };

        public Token(float constantValue, CompilerContext context)
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
