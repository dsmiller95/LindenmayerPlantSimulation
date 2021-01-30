using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Dman.LSystem.SystemCompiler
{
    public class ExpressionCompiler
    {
        public IDictionary<string, ParameterExpression> parameters;
        public ExpressionCompiler(Dictionary<string, ParameterExpression> parameters)
        {
            this.parameters = parameters;
        }
        public ExpressionCompiler(params string[] doubleParams)
        {
            parameters = doubleParams.ToDictionary(x => x, x => Expression.Parameter(typeof(double), x));
        }

        public static Delegate CompileExpressionToDelegateWithParameters(string expressionString, string[] namedNumericParameters)
        {
            var compiler = new ExpressionCompiler(namedNumericParameters);
            var expression = compiler.CompileToExpression(expressionString);
            // TODO: does this preserve parameter ordering?
            var lambdaExpr = Expression.Lambda(expression, compiler.parameters.Values.ToList());
            return lambdaExpr.Compile();
        }
        public static (Delegate, string) CompileExpressionToDelegateAndDescriptionWithParameters(string expressionString, string[] namedNumericParameters)
        {
            var compiler = new ExpressionCompiler(namedNumericParameters);
            var expression = compiler.CompileToExpression(expressionString);
            // TODO: does this preserve parameter ordering?
            var lambdaExpr = Expression.Lambda(expression, compiler.parameters.Values.ToList());
            return (lambdaExpr.Compile(), expression.ToString());
        }

        public Expression CompileToExpression(string expressionString)
        {
            var tokens = Tokenizer.Tokenize(expressionString, parameters.Keys.ToArray()).ToArray();
            var nestedExpression = GetHeirarchicalExpression(tokens);
            return nestedExpression.CompileSelfToExpression();
        }

        public TokenExpression GetHeirarchicalExpression(IEnumerable<Token> tokens)
        {
            Token lastReadSample = default;
            var enumerator = tokens.Select(x =>
            {
                lastReadSample = x;
                return x;
            }).GetEnumerator();
            enumerator.MoveNext();
            var firstToken = enumerator.Current;
            if (firstToken.token != TokenType.LEFT_PAREN)
            {
                throw new Exception("token string must begin with an open paren");
            }
            var internalExpressionList = ParseToTokenExpressionTillNextParen(enumerator).ToList();
            return new TokenExpression(
                internalExpressionList,
                new CompilerContext(firstToken.context, lastReadSample.context));
        }

        private TokenType expressions = TokenType.CONSTANT | TokenType.VARIABLE;

        /// <summary>
        /// assumes the open paren has already been consumed
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        private IEnumerable<TokenTarget> ParseToTokenExpressionTillNextParen(IEnumerator<Token> enumerator)
        {
            while (enumerator.MoveNext() && enumerator.Current.token != TokenType.RIGHT_PAREN)
            {
                var current = enumerator.Current;
                var tokenType = current.token;
                if (tokenType == TokenType.LEFT_PAREN)
                {
                    yield return new TokenExpression(
                        ParseToTokenExpressionTillNextParen(enumerator).ToList(),
                        new CompilerContext(current.context, enumerator.Current.context));
                }
                else if (expressions.HasFlag(tokenType))
                {
                    if (tokenType == TokenType.CONSTANT)
                    {
                        yield return new TokenExpression(Expression.Constant(current.value), current.context);
                    }
                    else if (tokenType == TokenType.VARIABLE)
                    {
                        if (!parameters.TryGetValue(current.name, out var parameterExp))
                        {

                            throw current.context.ExceptionHere($"no parameter found for '{current.name}'");
                        }
                        yield return new TokenExpression(parameterExp, current.context);
                    }
                    else
                    {
                        throw new Exception($"Invalid Expression Token Type: '{Enum.GetName(typeof(TokenType), tokenType)}'");
                    }
                }
                else
                {
                    // the token must be an operator
                    yield return new TokenOperator(current.context)
                    {
                        type = tokenType,
                    };
                }
            }
        }
    }
}
