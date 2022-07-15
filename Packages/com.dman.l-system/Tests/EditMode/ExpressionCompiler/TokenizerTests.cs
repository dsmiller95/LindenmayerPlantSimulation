using Dman.LSystem.SystemCompiler;
using NUnit.Framework;
using System.Linq;

public class TokenizerTests
{
    [Test]
    public void TokenizesNumericExpression()
    {
        var expressionString = "(2 + 0.4 - (4 * (2 - .1))/3 >= 3)";
        var tokens = Tokenizer.Tokenize(expressionString).ToArray();
        Assert.AreEqual(new Token[]
        {
            new Token(TokenType.LEFT_PAREN, new CompilerContext(0, 1)),
            new Token(2, new CompilerContext(1, 2)),
            new Token(TokenType.ADD, new CompilerContext(3, 4)),
            new Token(0.4f, new CompilerContext(5, 8)),
            new Token(TokenType.SUBTRACT, new CompilerContext(9, 10)),
            new Token(TokenType.LEFT_PAREN, new CompilerContext(11, 12)),
            new Token(4, new CompilerContext(12, 13)),
            new Token(TokenType.MULTIPLY, new CompilerContext(14, 15)),
            new Token(TokenType.LEFT_PAREN, new CompilerContext(16, 17)),
            new Token(2, new CompilerContext(17, 18)),
            new Token(TokenType.SUBTRACT, new CompilerContext(19, 20)),
            new Token(0.1f, new CompilerContext(21, 23)),
            new Token(TokenType.RIGHT_PAREN, new CompilerContext(23, 24)),
            new Token(TokenType.RIGHT_PAREN, new CompilerContext(24, 25)),
            new Token(TokenType.DIVIDE, new CompilerContext(25, 26)),
            new Token(3, new CompilerContext(26, 27)),
            new Token(TokenType.GREATER_THAN_OR_EQ, new CompilerContext(28, 30)),
            new Token(3, new CompilerContext(31, 32)),
            new Token(TokenType.RIGHT_PAREN, new CompilerContext(32, 33))
        }, tokens);
    }

    [Test]
    public void TokenizeNumericExpressionWithParameters()
    {
        var expressionString = "(2 + value - (x * (2 - .1))/3 >= 3)";
        var tokens = Tokenizer.Tokenize(expressionString, new string[] { "value", "x" }).ToArray();
        Assert.AreEqual(new Token[]
        {
            new Token(TokenType.LEFT_PAREN, new CompilerContext(0, 1)),
            new Token(2, new CompilerContext(1, 2)),
            new Token(TokenType.ADD, new CompilerContext(3, 4)),
            new Token("value", new CompilerContext(5, 10)),
            new Token(TokenType.SUBTRACT, new CompilerContext(11, 12)),
            new Token(TokenType.LEFT_PAREN, new CompilerContext(13, 14)),
            new Token("x", new CompilerContext(14, 15)),
            new Token(TokenType.MULTIPLY, new CompilerContext(16, 17)),
            new Token(TokenType.LEFT_PAREN, new CompilerContext(18, 19)),
            new Token(2, new CompilerContext(19, 20)),
            new Token(TokenType.SUBTRACT, new CompilerContext(21, 22)),
            new Token(0.1f, new CompilerContext(23, 25)),
            new Token(TokenType.RIGHT_PAREN, new CompilerContext(25, 26)),
            new Token(TokenType.RIGHT_PAREN, new CompilerContext(26, 27)),
            new Token(TokenType.DIVIDE, new CompilerContext(27, 28)),
            new Token(3, new CompilerContext(28, 29)),
            new Token(TokenType.GREATER_THAN_OR_EQ, new CompilerContext(30, 32)),
            new Token(3, new CompilerContext(33, 34)),
            new Token(TokenType.RIGHT_PAREN, new CompilerContext(34, 35))
        }, tokens);
    }
}
