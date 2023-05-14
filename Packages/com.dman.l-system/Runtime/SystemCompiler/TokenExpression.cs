using Dman.LSystem.SystemRuntime.DynamicExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Dman.LSystem.Extern;

namespace Dman.LSystem.SystemCompiler
{
    internal abstract class TokenTarget
    {
        public CompilerContext context;
        protected TokenTarget(CompilerContext context)
        {
            this.context = context;
        }
    }

    internal class TokenExpression : TokenTarget
    {
        public bool isTokenSeries;
        public OperatorBuilder compiledExpression;
        public List<TokenTarget> tokenSeries;

        public TokenExpression(OperatorBuilder expression, CompilerContext context) : base(context)
        {
            compiledExpression = expression;
            isTokenSeries = false;
        }

        public TokenExpression(List<TokenTarget> series, CompilerContext context) : base(context)
        {
            if (series.Count == 0)
            {
                throw context.ExceptionHere("Empty expression is not allowed");
            }

            tokenSeries = series;
            isTokenSeries = true;
        }

        public OperatorBuilder CompileSelfToExpression()
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
                        throw tokenSeries[i].context.ExceptionHere(
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
                if (tokenSeries.Count <= unaryIndex)
                {
                    throw op.context.ExceptionHere("Stranded Operator");
                }
                var value = tokenSeries[unaryIndex] as TokenExpression;
                var valuesExpression = value.CompileSelfToExpression();
                switch (op.type)
                {
                    case TokenType.SUBTRACT:
                        tokenSeries[unaryIndex] = new TokenExpression(OperatorBuilder.Unary(OperatorType.NegateUnary, valuesExpression), op.context);
                        break;
                    case TokenType.BOOLEAN_NOT:
                        tokenSeries[unaryIndex] = new TokenExpression(OperatorBuilder.Unary(OperatorType.BooleanNot, valuesExpression), op.context);
                        break;

                    default:
                        throw op.context.ExceptionHere($"Unsupported unary operator: {Enum.GetName(typeof(TokenType), op.type)}");
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
                        op,
                        secondVal.CompileSelfToExpression()),
                    firstVal.context);
                tokenLinkedList.Remove(firstVal);
                operatorNode.Value = newVal;
                tokenLinkedList.Remove(secondVal);
            }

            //should be all compiled
            if (tokenLinkedList.Count != 1)
            {
                if (tokenLinkedList.Count > 0)
                {
                    throw tokenLinkedList.First.Value.context.ExceptionHere("Compilation error: token string could not compile to one expression");
                }
                else
                {
                    throw new SyntaxException("Compilation error: token string could not compile to one expression");
                }
            }

            // the final node could be uncompiled if this was a simple nested paren. call compile method just in case, rely on short-circuit if already compiled.
            return (tokenLinkedList.First.Value as TokenExpression).CompileSelfToExpression();
        }

        private OperatorBuilder GetExpressionFromBinaryOperator(OperatorBuilder a, TokenOperator op, OperatorBuilder b)
        {
            OperatorType newOpType = default;

            switch (op.type)
            {
                case TokenType.MULTIPLY:
                    newOpType = OperatorType.Multiply;
                    break;
                case TokenType.DIVIDE:
                    newOpType = OperatorType.Divide;
                    break;
                case TokenType.REMAINDER:
                    newOpType = OperatorType.Remainder;
                    break;
                case TokenType.EXPONENT:
                    newOpType = OperatorType.Exponent;
                    break;
                case TokenType.ADD:
                    newOpType = OperatorType.Add;
                    break;
                case TokenType.SUBTRACT:
                    newOpType = OperatorType.Subtract;
                    break;

                case TokenType.GREATER_THAN:
                    newOpType = OperatorType.GreaterThan;
                    break;
                case TokenType.LESS_THAN:
                    newOpType = OperatorType.LessThan;
                    break;
                case TokenType.GREATER_THAN_OR_EQ:
                    newOpType = OperatorType.GreaterThanOrEq;
                    break;
                case TokenType.LESS_THAN_OR_EQ:
                    newOpType = OperatorType.LessThanOrEq;
                    break;

                case TokenType.EQUAL:
                    newOpType = OperatorType.Equal;
                    break;
                case TokenType.NOT_EQUAL:
                    newOpType = OperatorType.NotEqual;
                    break;

                case TokenType.BOOLEAN_AND:
                    newOpType = OperatorType.BooleanAnd;
                    break;
                case TokenType.BOOLEAN_OR:
                    newOpType = OperatorType.BooleanOr;
                    break;
                default:
                    throw op.context.ExceptionHere($"Invalid binary operator symbol: {Enum.GetName(typeof(TokenType), op.type)}");
            }
            return OperatorBuilder.Binary(newOpType, a, b);
        }
    }

    internal class TokenOperator : TokenTarget
    {
        public TokenType type;

        public TokenOperator(CompilerContext context) : base(context)
        {

        }
    }
}
