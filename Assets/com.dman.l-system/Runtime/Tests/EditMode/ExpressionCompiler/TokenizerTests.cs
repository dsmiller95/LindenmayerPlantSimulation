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
            new Token(TokenType.LEFT_PAREN, 0),
            new Token(2, 1),
            new Token(TokenType.ADD, 3),
            new Token(0.4, 5),
            new Token(TokenType.SUBTRACT, 9),
            new Token(TokenType.LEFT_PAREN, 11),
            new Token(4, 12),
            new Token(TokenType.MULTIPLY, 14),
            new Token(TokenType.LEFT_PAREN, 16),
            new Token(2, 17),
            new Token(TokenType.SUBTRACT, 19),
            new Token(0.1, 21),
            new Token(TokenType.RIGHT_PAREN, 23),
            new Token(TokenType.RIGHT_PAREN, 24),
            new Token(TokenType.DIVIDE, 25),
            new Token(3, 26),
            new Token(TokenType.GREATER_THAN_OR_EQ, 28),
            new Token(3, 31),
            new Token(TokenType.RIGHT_PAREN, 32)
        }, tokens);
    }

    [Test]
    public void TokenizeNumericExpressionWithParameters()
    {
        var expressionString = "(2 + value - (x * (2 - .1))/3 >= 3)";
        var tokens = Tokenizer.Tokenize(expressionString, new string[] {"value", "x"}).ToArray();
        Assert.AreEqual(new Token[]
        {
            new Token(TokenType.LEFT_PAREN, 0),
            new Token(2, 1),
            new Token(TokenType.ADD, 3),
            new Token("value", 5),
            new Token(TokenType.SUBTRACT, 11),
            new Token(TokenType.LEFT_PAREN, 13),
            new Token("x", 14),
            new Token(TokenType.MULTIPLY, 16),
            new Token(TokenType.LEFT_PAREN, 18),
            new Token(2, 19),
            new Token(TokenType.SUBTRACT, 21),
            new Token(0.1, 23),
            new Token(TokenType.RIGHT_PAREN, 25),
            new Token(TokenType.RIGHT_PAREN, 26),
            new Token(TokenType.DIVIDE, 27),
            new Token(3, 28),
            new Token(TokenType.GREATER_THAN_OR_EQ, 30),
            new Token(3, 33),
            new Token(TokenType.RIGHT_PAREN, 34)
        }, tokens);
    }
}
