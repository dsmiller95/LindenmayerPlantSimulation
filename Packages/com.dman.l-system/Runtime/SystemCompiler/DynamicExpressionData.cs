using Dman.LSystem.SystemRuntime.DynamicExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dman.LSystem.Extern;
using Unity.Collections;

namespace Dman.LSystem.SystemCompiler
{

    public class DynamicExpressionData
    {
        private List<OperatorDefinition> definitionList = new List<OperatorDefinition>();
        public string OgExpressionStringValue { get; private set; }
        public ushort OperatorSpaceNeeded => (ushort)definitionList.Count;

        public DynamicExpressionData(
            OperatorBuilder topOperator,
            ParameterExpression[] orderedParameters)
        {
            OgExpressionStringValue = topOperator.ToString();
            var builderQueue = new Queue<OperatorBuilder>();
            builderQueue.Enqueue(topOperator);

            while (builderQueue.Count > 0)
            {
                var nextNode = builderQueue.Dequeue();
                if (nextNode.type == OperatorType.ConstantValue)
                {
                    definitionList.Add(new OperatorDefinition
                    {
                        operator_type = OperatorType.ConstantValue,
                        node_value = nextNode.nodeValue
                    });
                }
                else if (nextNode.type == OperatorType.ParameterValue)
                {
                    var paramIndex = Array.IndexOf(orderedParameters, nextNode.parameter);
                    definitionList.Add(new OperatorDefinition
                    {
                        operator_type = OperatorType.ParameterValue,
                        parameter_index = paramIndex
                    });
                }
                else if (nextNode.IsUnary)
                {
                    var rhsIndex = definitionList.Count + builderQueue.Count + 1;
                    definitionList.Add(new OperatorDefinition
                    {
                        operator_type = nextNode.type,
                        rhs = (ushort)rhsIndex
                    });
                    builderQueue.Enqueue(nextNode.rhs);
                }
                else if (nextNode.IsBinary)
                {
                    var lhsIndex = definitionList.Count + builderQueue.Count + 1;
                    var rhsIndex = definitionList.Count + builderQueue.Count + 2;
                    definitionList.Add(new OperatorDefinition
                    {
                        operator_type = nextNode.type,
                        lhs = (ushort)lhsIndex,
                        rhs = (ushort)rhsIndex
                    });
                    builderQueue.Enqueue(nextNode.lhs);
                    builderQueue.Enqueue(nextNode.rhs);
                }
                else
                {
                    throw new Exception("unrecognized node type");
                }
            }
        }

        public StructExpression WriteIntoOpDataArray(
            NativeArray<OperatorDefinition> dataArray,
            JaggedIndexing opSpace)
        {
            if (opSpace.length != OperatorSpaceNeeded)
            {
                throw new Exception("not enough space");
            }
            for (int i = 0; i < opSpace.length; i++)
            {
                dataArray[i + opSpace.index] = definitionList[i];
            }
            return new StructExpression
            {
                operationDataSlice = opSpace
            };
        }

        public float DynamicInvoke(params float[] input)
        {
            using var inputParams = new NativeArray<float>(input, Allocator.Temp);
            using var opDefs = new NativeArray<OperatorDefinition>(definitionList.ToArray(), Allocator.Temp);

            var structExp = new StructExpression
            {
                operationDataSlice = new JaggedIndexing
                {
                    index = 0,
                    length = (ushort)opDefs.Length
                }
            };

            return StructExpression.EvaluateExpression(structExp, inputParams,
                new JaggedIndexing
                {
                    index = 0,
                    length = (ushort)inputParams.Length
                },
                opDefs);
        }
    }

    public class OperatorBuilder
    {
        public OperatorType type;
        public float nodeValue;
        public ParameterExpression parameter;
        public OperatorBuilder lhs;
        public OperatorBuilder rhs;

        public bool IsUnary => lhs == null && rhs != null;
        public bool IsBinary => lhs != null && rhs != null;

        private OperatorBuilder()
        { }

        public static OperatorBuilder ConstantValue(float constant)
        {
            return new OperatorBuilder
            {
                type = OperatorType.ConstantValue,
                nodeValue = constant
            };
        }
        public static OperatorBuilder ParameterReference(ParameterExpression parameterIndex)
        {
            return new OperatorBuilder
            {
                type = OperatorType.ParameterValue,
                parameter = parameterIndex
            };
        }
        public static OperatorBuilder Unary(OperatorType opType, OperatorBuilder single)
        {
            if (opType != OperatorType.BooleanNot && opType != OperatorType.NegateUnary)
            {
                throw new SyntaxException($"Unsupported Unary Operator {Enum.GetName(typeof(OperatorType), opType)}");
            }
            return new OperatorBuilder
            {
                type = opType,
                rhs = single,
            };
        }
        public static OperatorBuilder Binary(OperatorType opType, OperatorBuilder lhs, OperatorBuilder rhs)
        {
            if (opType == OperatorType.BooleanNot
                || opType == OperatorType.NegateUnary
                || opType == OperatorType.ConstantValue
                || opType == OperatorType.ParameterValue)
            {
                throw new SyntaxException($"Unsupported Binary Operator {Enum.GetName(typeof(OperatorType), opType)}");
            }
            return new OperatorBuilder
            {
                type = opType,
                rhs = rhs,
                lhs = lhs
            };
        }


        public Expression CompileToLinqExpression()
        {
            var rhsComp = rhs?.CompileToLinqExpression() ?? null;
            var lhsComp = lhs?.CompileToLinqExpression() ?? null;
            switch (type)
            {
                case OperatorType.ConstantValue:
                    return Expression.Constant(nodeValue);
                case OperatorType.ParameterValue:
                    return parameter;
                case OperatorType.Multiply:
                    return Expression.Multiply(lhsComp, rhsComp);
                case OperatorType.Divide:
                    return Expression.Divide(lhsComp, rhsComp);
                case OperatorType.Add:
                    return Expression.AddChecked(lhsComp, rhsComp);
                case OperatorType.Subtract:
                    return Expression.SubtractChecked(lhsComp, rhsComp);
                case OperatorType.Remainder:
                    return Expression.Modulo(lhsComp, rhsComp);
                case OperatorType.Exponent:
                    return Expression.Convert( // cast to double and then back, because Expression.Power is a proxy for Math.Pow
                        Expression.Power(
                            Expression.Convert(lhsComp, typeof(double)),
                            Expression.Convert(rhsComp, typeof(double))
                        ),
                        typeof(float)
                    );
                case OperatorType.GreaterThan:
                    return Expression.GreaterThan(lhsComp, rhsComp);
                case OperatorType.LessThan:
                    return Expression.LessThan(lhsComp, rhsComp);
                case OperatorType.GreaterThanOrEq:
                    return Expression.GreaterThanOrEqual(lhsComp, rhsComp);
                case OperatorType.LessThanOrEq:
                    return Expression.LessThanOrEqual(lhsComp, rhsComp);
                case OperatorType.Equal:
                    return Expression.Equal(lhsComp, rhsComp);
                case OperatorType.NotEqual:
                    return Expression.NotEqual(lhsComp, rhsComp);
                case OperatorType.BooleanAnd:
                    return Expression.AndAlso(lhsComp, rhsComp);
                case OperatorType.BooleanOr:
                    return Expression.OrElse(lhsComp, rhsComp);

                case OperatorType.BooleanNot:
                    return Expression.Not(rhsComp);
                case OperatorType.NegateUnary:
                    return Expression.NegateChecked(rhsComp);

                default:
                    break;
            }
            throw new SyntaxException($"Invalid binary operator symbol: {Enum.GetName(typeof(OperatorType), type)}");
        }

        public override string ToString()
        {
            if (type == OperatorType.ConstantValue)
            {
                return nodeValue.ToString("f1");
            }
            if (type == OperatorType.ParameterValue)
            {
                return $"PARAMAT({parameter})";
            }
            var str = Enum.GetName(typeof(OperatorType), type) + "(";
            if (lhs != null)
            {
                str += lhs.ToString() + ", ";
            }
            if (rhs != null)
            {
                str += rhs.ToString();
            }
            str += ")";
            return str;
        }
    }
}
