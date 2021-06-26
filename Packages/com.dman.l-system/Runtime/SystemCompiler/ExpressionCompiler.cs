using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Dman.LSystem.SystemCompiler
{
    internal class ExpressionCompiler
    {
        public IDictionary<string, ParameterExpression> parameters;
        public ExpressionCompiler(Dictionary<string, ParameterExpression> parameters)
        {
            this.parameters = parameters;
        }
        public ExpressionCompiler(params string[] floatParams)
        {
            parameters = new Dictionary<string, ParameterExpression>();
            foreach (var parameterName in floatParams)
            {
                if (parameters.ContainsKey(parameterName))
                {
                    throw new SyntaxException($"Attempted to declare the same parameter twice: '{parameterName}'");
                }
                parameters[parameterName] = Expression.Parameter(typeof(float), parameterName);
            }
        }

        public static DynamicExpressionData CompileExpressionToDelegateWithParameters(string expressionString, string[] namedNumericParameters = null)
        {
            var compiler = new ExpressionCompiler(namedNumericParameters ?? new string[0]);
            var expression = compiler.CompileToExpression(expressionString);
            return new DynamicExpressionData(expression, compiler.parameters.Values.ToArray());
        }

        public OperatorBuilder CompileToExpression(string expressionString)
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
                throw firstToken.context.ExceptionHere("token string must begin with an open paren");
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
                        yield return new TokenExpression(OperatorBuilder.ConstantValue(current.value), current.context);
                    }
                    else if (tokenType == TokenType.VARIABLE)
                    {
                        if (!parameters.TryGetValue(current.name, out var parameterExp))
                        {
                            throw current.context.ExceptionHere($"no parameter found for '{current.name}'");
                        }
                        yield return new TokenExpression(OperatorBuilder.ParameterReference(parameterExp), current.context);
                    }
                    else
                    {
                        throw current.context.ExceptionHere($"Invalid Expression Token Type: '{Enum.GetName(typeof(TokenType), tokenType)}'");
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
