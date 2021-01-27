using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.ExpressionCompiler
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
            this.parameters = doubleParams.ToDictionary(x => x, x => Expression.Parameter(typeof(double), x));
        }

        public Expression CompileToExpression(string expressionString)
        {
            var tokens = Tokenizer.Tokenize(expressionString, parameters.Keys.ToArray()).ToArray();
            var nestedExpression = GetHeirarchicalExpression(tokens);
            return nestedExpression.CompileSelfToExpression();
        }

        public TokenExpression GetHeirarchicalExpression(IEnumerable<Token> tokens)
        {
            var enumerator = tokens.GetEnumerator();
            enumerator.MoveNext();
            if(enumerator.Current.token != TokenType.LEFT_PAREN)
            {
                throw new Exception("token string must begin with an open paren");
            }
            return new TokenExpression(ParseToTokenExpressionTillNextParen(enumerator).ToList());
        }

        private TokenType expressions = TokenType.CONSTANT | TokenType.VARIABLE;

        /// <summary>
        /// assumes the open paren has alread been consumed
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        private IEnumerable<TokenTarget> ParseToTokenExpressionTillNextParen(IEnumerator<Token> enumerator)
        {
            while (enumerator.MoveNext() && enumerator.Current.token != TokenType.RIGHT_PAREN)
            {
                var current = enumerator.Current;
                var tokenType = current.token;
                if(tokenType == TokenType.LEFT_PAREN)
                {
                    yield return new TokenExpression(ParseToTokenExpressionTillNextParen(enumerator).ToList());
                }else if (expressions.HasFlag(tokenType))
                {
                    if(tokenType == TokenType.CONSTANT)
                    {
                        yield return new TokenExpression(Expression.Constant(enumerator.Current.value));
                    }else if (tokenType == TokenType.VARIABLE)
                    {
                        if (!this.parameters.TryGetValue(current.name, out var parameterExp)) {
                            throw new Exception($"no parameter found for '{current.name}'");
                        }
                        yield return new TokenExpression(parameterExp);
                    }else
                    {
                        throw new Exception($"no parameter found for '{Enum.GetName(typeof(TokenType), tokenType)}'");
                    }
                }else
                {
                    // the token must be an operator
                    yield return new TokenOperator
                    {
                        type = tokenType
                    };
                }
            }
        }
    }
}
