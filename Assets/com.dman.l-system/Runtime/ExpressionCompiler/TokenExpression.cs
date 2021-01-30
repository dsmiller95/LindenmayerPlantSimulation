﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Dman.LSystem.ExpressionCompiler
{
    public abstract class TokenTarget
    {
        public int originalStringIndex;
        public int originalStringEndIndex;
        protected TokenTarget(int originalIndex) : this(originalIndex, originalIndex + 1)
        {
        }
        protected TokenTarget(int originalIndex, int endIndex)
        {
            this.originalStringIndex = originalIndex;
            this.originalStringEndIndex = endIndex;
        }

        public SyntaxException ExceptionHere(string message)
        {
            return new SyntaxException(message, originalStringIndex, originalStringEndIndex - originalStringIndex);
        }
    }

    public class TokenExpression : TokenTarget
    {
        public bool isTokenSeries;
        public Expression compiledExpression;
        public List<TokenTarget> tokenSeries;

        public TokenExpression(Expression expression, int index): base(index)
        {
            compiledExpression = expression;
            isTokenSeries = false;
        }

        public TokenExpression(List<TokenTarget> series, int index, int endIndex): base(index, endIndex)
        {
            if(series.Count == 0)
            {
                throw new SyntaxException("Empty parentheses are not allowed", index, (endIndex + 1) - index);
            }

            tokenSeries = series;
            isTokenSeries = true;
        }

        public Expression CompileSelfToExpression()
        {
            if (!isTokenSeries)
            {
                return compiledExpression;
            }

            // detect unaries
            int totalPrecedingOperators = 1;
            var unaryOperatorIndexes = new List<int>();
            for (int i = 0; i < tokenSeries.Count; i++)
            {
                if (tokenSeries[i] is TokenOperator)
                {
                    totalPrecedingOperators++;
                    if (totalPrecedingOperators == 2)
                    {
                        unaryOperatorIndexes.Add(i);
                    }
                    else if (totalPrecedingOperators > 2)
                    {
                        throw tokenSeries[i].ExceptionHere(
                            $"{totalPrecedingOperators} consecutive operators detected");
                    }
                }
                else
                {
                    totalPrecedingOperators = 0;
                }
            }

            foreach (var unaryIndex in ((IEnumerable<int>)unaryOperatorIndexes).Reverse())
            {
                var op = tokenSeries[unaryIndex] as TokenOperator;
                tokenSeries.RemoveAt(unaryIndex);
                if(tokenSeries.Count <= unaryIndex)
                {
                    throw op.ExceptionHere("Stranded Operator");
                }
                var value = tokenSeries[unaryIndex] as TokenExpression;
                var valuesExpression = value.CompileSelfToExpression();
                switch (op.type)
                {
                    case TokenType.SUBTRACT:
                        tokenSeries[unaryIndex] = new TokenExpression(Expression.NegateChecked(valuesExpression), op.originalStringIndex);
                        break;
                    default:
                        throw op.ExceptionHere($"Unsupported unary operator: {Enum.GetName(typeof(TokenType), op.type)}");
                }
            }

            var operatorNodes = new List<LinkedListNode<TokenTarget>>();
            var tokenLinkedList = new LinkedList<TokenTarget>(tokenSeries);
            var currentNode = tokenLinkedList.First;
            do
            {
                if (currentNode.Value is TokenOperator op)
                {
                    operatorNodes.Add(currentNode);
                }
            } while ((currentNode = currentNode.Next) != null);

            operatorNodes.Sort((a, b) => Token.OPERATOR_PRECIDENCE[(a.Value as TokenOperator).type] - Token.OPERATOR_PRECIDENCE[(b.Value as TokenOperator).type]);

            foreach (var operatorNode in operatorNodes)
            {
                var firstVal = operatorNode.Previous.Value as TokenExpression;
                var op = operatorNode.Value as TokenOperator;
                var secondVal = operatorNode.Next.Value as TokenExpression;

                var newVal = new TokenExpression(
                    GetExpressionFromBinaryOperator(
                        firstVal.CompileSelfToExpression(),
                        op.type,
                        secondVal.CompileSelfToExpression()),
                    firstVal.originalStringIndex);
                tokenLinkedList.Remove(firstVal);
                operatorNode.Value = newVal;
                tokenLinkedList.Remove(secondVal);
            }

            //should be all compiled
            if (tokenLinkedList.Count != 1)
            {
                throw new Exception("Compilation error: token string could not compile to one expression");
            }
            return (tokenLinkedList.First.Value as TokenExpression).compiledExpression;
        }

        private Expression GetExpressionFromBinaryOperator(Expression a, TokenType op, Expression b)
        {
            switch (op)
            {
                case TokenType.MULTIPLY:
                    return Expression.MultiplyChecked(a, b);
                case TokenType.DIVIDE:
                    return Expression.Divide(a, b);
                case TokenType.ADD:
                    return Expression.AddChecked(a, b);
                case TokenType.SUBTRACT:
                    return Expression.SubtractChecked(a, b);
                case TokenType.EXPONENT:
                    return Expression.Power(a, b);
                case TokenType.GREATER_THAN:
                    return Expression.GreaterThan(a, b);
                case TokenType.LESS_THAN:
                    return Expression.LessThan(a, b);
                case TokenType.GREATER_THAN_OR_EQ:
                    return Expression.GreaterThanOrEqual(a, b);
                case TokenType.LESS_THAN_OR_EQ:
                    return Expression.LessThanOrEqual(a, b);
                default:
                    throw new SyntaxException($"Invalid binary operator symbol: {Enum.GetName(typeof(TokenType), op)}");
            }
        }
    }

    public class TokenOperator : TokenTarget
    {
        public TokenType type;

        public TokenOperator(int index): base(index)
        {

        }
    }
}
