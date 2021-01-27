using Dman.LSystem;
using Dman.LSystem.ExpressionCompiler;
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
            new Token(TokenType.LEFT_PAREN),
            new Token(2),
            new Token(TokenType.ADD),
            new Token(0.4),
            new Token(TokenType.SUBTRACT),
            new Token(TokenType.LEFT_PAREN),
            new Token(4),
            new Token(TokenType.MULTIPLY),
            new Token(TokenType.LEFT_PAREN),
            new Token(2),
            new Token(TokenType.SUBTRACT),
            new Token(0.1),
            new Token(TokenType.RIGHT_PAREN),
            new Token(TokenType.RIGHT_PAREN),
            new Token(TokenType.DIVIDE),
            new Token(3),
            new Token(TokenType.GREATER_THAN_OR_EQ),
            new Token(3),
            new Token(TokenType.RIGHT_PAREN)
        }, tokens);
    }

    [Test]
    public void TokenizeNumericExpressionWithParameters()
    {
        var expressionString = "(2 + value - (x * (2 - .1))/3 >= 3)";
        var tokens = Tokenizer.Tokenize(expressionString, new string[] {"value", "x"}).ToArray();
        Assert.AreEqual(new Token[]
        {
            new Token(TokenType.LEFT_PAREN),
            new Token(2),
            new Token(TokenType.ADD),
            new Token("value"),
            new Token(TokenType.SUBTRACT),
            new Token(TokenType.LEFT_PAREN),
            new Token("x"),
            new Token(TokenType.MULTIPLY),
            new Token(TokenType.LEFT_PAREN),
            new Token(2),
            new Token(TokenType.SUBTRACT),
            new Token(0.1),
            new Token(TokenType.RIGHT_PAREN),
            new Token(TokenType.RIGHT_PAREN),
            new Token(TokenType.DIVIDE),
            new Token(3),
            new Token(TokenType.GREATER_THAN_OR_EQ),
            new Token(3),
            new Token(TokenType.RIGHT_PAREN)
        }, tokens);
    }
}
