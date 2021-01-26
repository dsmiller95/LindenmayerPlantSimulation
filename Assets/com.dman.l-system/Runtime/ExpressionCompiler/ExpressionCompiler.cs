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

        public static Expression CompileTokens(IEnumerable<Token> tokens)
        {
            var tokenList = tokens.ToList();
            var symbolString = new SymbolString<object>(
                tokenList.Select(x => (int)x.token).ToArray(),
                tokenList.Select(x =>
                {
                    switch (x.token)
                    {
                        case TokenType.CONSTANT:
                            return new object[] { x.value };
                        case TokenType.VARIABLE:
                            return new object[] { x.name };
                        default:
                            return new object[0];
                    }
                }).ToArray());
            return null;
        }
    }

    public class CompilerReplacementRule
    {

    }
}
