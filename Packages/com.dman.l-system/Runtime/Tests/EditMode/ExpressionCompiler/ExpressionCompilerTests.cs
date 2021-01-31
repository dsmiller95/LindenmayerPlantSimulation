using Dman.LSystem.SystemCompiler;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public class ExpressionCompilerTests
{
    [Test]
    public void CompilesNestedExpressionsToHeirarchy()
    {
        var expressionString = "(2 - (4 * (2 - .1))/3 >= 3)";
        var tokens = Tokenizer.Tokenize(expressionString).ToArray();
        var expressionCompiler = new ExpressionCompiler(new Dictionary<string, ParameterExpression>());
        var nestedExpression = expressionCompiler.GetHeirarchicalExpression(tokens);
        Assert.AreEqual(7, nestedExpression.tokenSeries.Count);
        Assert.AreEqual(2, ((nestedExpression.tokenSeries[0] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
        Assert.AreEqual(TokenType.SUBTRACT, (nestedExpression.tokenSeries[1] as TokenOperator)?.type);
        Assert.IsTrue((nestedExpression.tokenSeries[2] as TokenExpression)?.isTokenSeries);
        Assert.AreEqual(TokenType.DIVIDE, (nestedExpression.tokenSeries[3] as TokenOperator)?.type);
        Assert.AreEqual(3, ((nestedExpression.tokenSeries[4] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
        Assert.AreEqual(TokenType.GREATER_THAN_OR_EQ, (nestedExpression.tokenSeries[5] as TokenOperator)?.type);
        Assert.AreEqual(3, ((nestedExpression.tokenSeries[6] as TokenExpression)?.compiledExpression as ConstantExpression).Value);

        var innerSeries1 = (nestedExpression.tokenSeries[2] as TokenExpression).tokenSeries;
        Assert.AreEqual(4, ((innerSeries1[0] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
        Assert.AreEqual(TokenType.MULTIPLY, (innerSeries1[1] as TokenOperator)?.type);
        Assert.IsTrue((innerSeries1[2] as TokenExpression)?.isTokenSeries);

        var innerSeries2 = (innerSeries1[2] as TokenExpression).tokenSeries;
        Assert.AreEqual(2, ((innerSeries2[0] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
        Assert.AreEqual(TokenType.SUBTRACT, (innerSeries2[1] as TokenOperator)?.type);
        Assert.AreEqual(0.1d, ((innerSeries2[2] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
    }
    [Test]
    public void CompilesNestedExpressionWithParameterToHeirarchy()
    {
        var expressionString = "(2 - (4 * vary))";

        var expressionCompiler = new ExpressionCompiler("vary");
        var tokens = Tokenizer.Tokenize(expressionString, expressionCompiler.parameters.Keys.ToArray()).ToArray();
        var nestedExpression = expressionCompiler.GetHeirarchicalExpression(tokens);

        Assert.AreEqual(3f, nestedExpression.tokenSeries.Count);
        Assert.AreEqual(2f, ((nestedExpression.tokenSeries[0] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
        Assert.AreEqual(TokenType.SUBTRACT, (nestedExpression.tokenSeries[1] as TokenOperator)?.type);
        Assert.IsTrue((nestedExpression.tokenSeries[2] as TokenExpression)?.isTokenSeries);

        var innerSeries1 = (nestedExpression.tokenSeries[2] as TokenExpression).tokenSeries;
        Assert.AreEqual(4f, ((innerSeries1[0] as TokenExpression)?.compiledExpression as ConstantExpression).Value);
        Assert.AreEqual(TokenType.MULTIPLY, (innerSeries1[1] as TokenOperator)?.type);
        Assert.AreEqual("vary", ((innerSeries1[2] as TokenExpression)?.compiledExpression as ParameterExpression).Name);
    }
    [Test]
    public void CompilesExpressionToSingleExpression()
    {
        var expressionString = "(2 - 4 * .5 + 1)";

        var parameters = new Dictionary<string, ParameterExpression>();
        var expressionCompiler = new ExpressionCompiler(parameters);
        var expression = expressionCompiler.CompileToExpression(expressionString);

        LambdaExpression le = Expression.Lambda(expression, parameters.Values.ToList());
        var compiledExpression = le.Compile();
        double result = (double)compiledExpression.DynamicInvoke();

        Assert.AreEqual(1f, result);
    }
    [Test]
    public void CompilesExpressionWithParameters()
    {
        var expressionString = "(2^pow + add)";

        var expressionCompiler = new ExpressionCompiler("pow", "add");
        var expression = expressionCompiler.CompileToExpression(expressionString);

        LambdaExpression le = Expression.Lambda(expression, expressionCompiler.parameters.Values.ToList());
        var compiledExpression = le.Compile();
        Assert.AreEqual(4d, (double)compiledExpression.DynamicInvoke(2d, 0d));
        Assert.AreEqual(2d, (double)compiledExpression.DynamicInvoke(0d, 1d));
        Assert.AreEqual(9f, (double)compiledExpression.DynamicInvoke(3d, 1d));
        Assert.AreEqual(Math.Sqrt(2) + 10, (double)compiledExpression.DynamicInvoke(0.5d, 10d));
        Assert.AreEqual((1 << 10), (double)compiledExpression.DynamicInvoke(10d, 0d));
    }
    [Test]
    public void CompilesExpressionWithEveryOperator()
    {
        var expressionString = "(2 * -3 < 4^2 && 3 % 5 > 4 - 2 || !(8 / 3 <= 2 + 1.9 && 2 >= 3) && 3 == 3 && 2 != 3)";

        var parameters = new Dictionary<string, ParameterExpression>();
        var expressionCompiler = new ExpressionCompiler(parameters);
        var expression = expressionCompiler.CompileToExpression(expressionString);

        LambdaExpression le = Expression.Lambda(expression, parameters.Values.ToList());
        var compiledExpression = le.Compile();
        var result = compiledExpression.DynamicInvoke();
        Assert.IsAssignableFrom<bool>(result);
        Assert.AreEqual(true, (bool)result);
    }
}
