using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using NUnit.Framework;
using System.Linq.Expressions;
using Dman.LSystem.Extern;
using Unity.Collections;
using Expression = System.Linq.Expressions.Expression;
using OperatorDefinition = Dman.LSystem.SystemRuntime.DynamicExpressions.OperatorDefinition;
using OperatorType = Dman.LSystem.SystemRuntime.DynamicExpressions.OperatorType;

public class DynamicExpressionEvaluationTest
{
    [Test]
    public void EvaluatesBasicExpressionWithConstantAndParameter()
    {
        using var inputParams = new NativeArray<float>(new float[] { 2f }, Allocator.Persistent);
        using var operatorData = new NativeArray<OperatorDefinition>(new OperatorDefinition[]
        {
            new OperatorDefinition
            {
                operatorType = OperatorType.MULTIPLY,
                rhs = 1,
                lhs = 2
            },
            new OperatorDefinition
            {
                operatorType = OperatorType.CONSTANT_VALUE,
                nodeValue = 1.5f
            },
            new OperatorDefinition
            {
                operatorType = OperatorType.PARAMETER_VALUE,
                parameterIndex = 0
            },
        }, Allocator.Persistent);

        var expression = new StructExpression
        {
            operationDataSlice = new JaggedIndexing
            {
                index = 0,
                length = 3
            }
        };

        var result = expression.EvaluateExpression(
            inputParams,
            new JaggedIndexing
            {
                index = 0,
                length = 1
            },
            operatorData);

        Assert.AreEqual(3f, result);
    }

    [Test]
    public void BuildsLinqExpressionFromBuilderTree()
    {
        var paramInput = Expression.Parameter(typeof(float), "testP");
        var operatorData = OperatorBuilder.Binary(OperatorType.MULTIPLY,
            OperatorBuilder.ConstantValue(1.5f),
            OperatorBuilder.ParameterReference(paramInput));

        var expression = operatorData.CompileToLinqExpression();
        var lambdaExpr = Expression.Lambda(expression, paramInput);
        var fun = lambdaExpr.Compile();

        var result = fun.DynamicInvoke(2f);

        Assert.AreEqual(3f, result);
    }
    [Test]
    public void BuildsStructExpressionFromBuilderTree()
    {
        var paramInput = Expression.Parameter(typeof(float), "testP");
        var operatorData = OperatorBuilder.Binary(OperatorType.MULTIPLY,
            OperatorBuilder.ConstantValue(1.5f),
            OperatorBuilder.ParameterReference(paramInput));

        var builder = new DynamicExpressionData(operatorData, new ParameterExpression[] { paramInput });

        using var nativeOpData = new NativeArray<OperatorDefinition>(builder.OperatorSpaceNeeded, Allocator.Persistent);
        using var inputParams = new NativeArray<float>(new float[] { 2f }, Allocator.Persistent);
        var opDataSpace = new JaggedIndexing
        {
            index = 0,
            length = builder.OperatorSpaceNeeded
        };
        var expression = builder.WriteIntoOpDataArray(
            nativeOpData,
            opDataSpace);

        var result = expression.EvaluateExpression(
            inputParams,
            new JaggedIndexing
            {
                index = 0,
                length = 1
            },
            nativeOpData);

        Assert.AreEqual(3f, result);
    }
}
