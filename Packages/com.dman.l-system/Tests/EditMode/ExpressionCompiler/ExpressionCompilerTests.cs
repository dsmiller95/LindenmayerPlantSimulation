using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime.NativeCollections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Unity.Collections;
using UnityEngine;

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
        Assert.AreEqual(2, ((nestedExpression.tokenSeries[0] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
        Assert.AreEqual(TokenType.SUBTRACT, (nestedExpression.tokenSeries[1] as TokenOperator)?.type);
        Assert.IsTrue((nestedExpression.tokenSeries[2] as TokenExpression)?.isTokenSeries);
        Assert.AreEqual(TokenType.DIVIDE, (nestedExpression.tokenSeries[3] as TokenOperator)?.type);
        Assert.AreEqual(3, ((nestedExpression.tokenSeries[4] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
        Assert.AreEqual(TokenType.GREATER_THAN_OR_EQ, (nestedExpression.tokenSeries[5] as TokenOperator)?.type);
        Assert.AreEqual(3, ((nestedExpression.tokenSeries[6] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);

        var innerSeries1 = (nestedExpression.tokenSeries[2] as TokenExpression).tokenSeries;
        Assert.AreEqual(4, ((innerSeries1[0] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
        Assert.AreEqual(TokenType.MULTIPLY, (innerSeries1[1] as TokenOperator)?.type);
        Assert.IsTrue((innerSeries1[2] as TokenExpression)?.isTokenSeries);

        var innerSeries2 = (innerSeries1[2] as TokenExpression).tokenSeries;
        Assert.AreEqual(2, ((innerSeries2[0] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
        Assert.AreEqual(TokenType.SUBTRACT, (innerSeries2[1] as TokenOperator)?.type);
        Assert.AreEqual(0.1f, ((innerSeries2[2] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
    }
    [Test]
    public void CompilesNestedExpressionWithParameterToHeirarchy()
    {
        var expressionString = "(2 - (4 * vary))";

        var expressionCompiler = new ExpressionCompiler("vary");
        var tokens = Tokenizer.Tokenize(expressionString, expressionCompiler.parameters.Keys.ToArray()).ToArray();
        var nestedExpression = expressionCompiler.GetHeirarchicalExpression(tokens);

        Assert.AreEqual(3f, nestedExpression.tokenSeries.Count);
        Assert.AreEqual(2f, ((nestedExpression.tokenSeries[0] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
        Assert.AreEqual(TokenType.SUBTRACT, (nestedExpression.tokenSeries[1] as TokenOperator)?.type);
        Assert.IsTrue((nestedExpression.tokenSeries[2] as TokenExpression)?.isTokenSeries);

        var innerSeries1 = (nestedExpression.tokenSeries[2] as TokenExpression).tokenSeries;
        Assert.AreEqual(4f, ((innerSeries1[0] as TokenExpression)?.compiledExpression as OperatorBuilder).nodeValue);
        Assert.AreEqual(TokenType.MULTIPLY, (innerSeries1[1] as TokenOperator)?.type);
        Assert.AreEqual("vary", ((innerSeries1[2] as TokenExpression)?.compiledExpression as OperatorBuilder).parameter.Name);
    }

    private void AssertFunctionResults(string expressionString, float[,] calls, object[] results, string[] paramNames = null)
    {
        var expressionCompiler = new ExpressionCompiler(paramNames == null ? new string[0] : paramNames);
        var operatorData = expressionCompiler.CompileToExpression(expressionString);
        var builder = new DynamicExpressionData(operatorData, expressionCompiler.parameters.Values.ToArray());

        using var nativeOpData = new NativeArray<OperatorDefinition>(builder.OperatorSpaceNeeded, Allocator.Persistent);

        var paramSize = (ushort)calls.GetLength(1);
        var inputParams = new NativeArray<float>(paramSize, Allocator.Persistent);
        var opDataSpace = new JaggedIndexing
        {
            index = 0,
            length = builder.OperatorSpaceNeeded
        };
        var expression = builder.WriteIntoOpDataArray(
            nativeOpData,
            opDataSpace);

        for (int call = 0; call < calls.GetLength(0); call++)
        {
            for (int param = 0; param < calls.GetLength(1); param++)
            {
                inputParams[param] = calls[call, param];
            }
            var result = StructExpression.EvaluateExpression(expression, inputParams,
                new JaggedIndexing
                {
                    index = 0,
                    length = paramSize
                },
                nativeOpData);
            if (results[call] is float floatVal)
            {
                Assert.AreEqual(result, floatVal);
            }
            else if (results[call] is bool boolValue)
            {
                if (boolValue)
                {
                    Assert.IsTrue(result > 0);
                }
                else
                {
                    Assert.IsFalse(result > 0);
                }
            }
        }

        inputParams.Dispose();
    }

    [Test]
    public void CompilesExpressionToSingleExpression()
    {
        AssertFunctionResults(
            "(2 - 4 * .5 + 1)",
            new float[1, 0],
            new object[] { 1f }
            );
    }

    [Test]
    public void CompilesBooleanExpressions()
    {
        AssertFunctionResults(
            "(!(2 > 4))",
            new float[1, 0],
            new object[] { 1f }
            );
        AssertFunctionResults(
            "(!(2 > 4) && !(4 - 3 >= 0))",
            new float[1, 0],
            new object[] { 0f }
            );
    }
    [Test]
    public void CompilesExpressionWithExtraParensOutside()
    {
        AssertFunctionResults(
            "((1 + 1))",
            new float[1, 0],
            new object[] { 2f }
            );
    }
    [Test]
    public void CompilesExpressionWithExtraParensInside()
    {
        AssertFunctionResults(
            "(((1 - 2)) * ((1 + 1)))",
            new float[1, 0],
            new object[] { -2f }
            );
    }
    [Test]
    public void CompilesExpressionWithExtraParensEverywhere()
    {
        AssertFunctionResults(
            "((((1 - 2)) * ((1 + 1))))",
            new float[1, 0],
            new object[] { -2f }
            );
    }
    [Test]
    public void CompilesExpressionWithExtraParensEverywhereAndUnaries()
    {
        AssertFunctionResults(
            "(-((-(1 - 2)) * -((1 + 1))))",
            new float[1, 0],
            new object[] { 2f }
            );
    }
    [Test]
    public void CompilesExpressionWithParameters()
    {
        AssertFunctionResults(
            "(2^pow + add)",
            new float[5, 2] {
                {2  ,  0 },
                {0  ,  1 },
                {3  ,  1 },
                {.5f, 10 },
                {10 ,  0 },
                },
            new object[] {
                4f,
                2f,
                9f,
                Mathf.Sqrt(2) + 10,
                1 << 10
            },
            new string[]
            {
                "pow",
                "add"
            }
            );
    }
    [Test]
    public void CompilesExpressionWithEveryOperator()
    {
        AssertFunctionResults(
            "(2 * -3 < 4^2 && 3 % 5 > 4 - 2 || !(8 / 3 <= 2 + 1.9 && 2 >= 3) && 3 == 3 && 2 != 3)",
            new float[1, 0],
            new object[] { true }
            );
    }
}
