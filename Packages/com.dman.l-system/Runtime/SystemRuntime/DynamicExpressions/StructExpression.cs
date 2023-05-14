using Dman.LSystem.Extern;
using Dman.LSystem.Extern.Adapters;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DynamicExpressions
{
    /// <summary>
    /// StructExpression represents a slice over packed list of OperatorDefinitions. Index 0 is the root operator,
    /// and all other contained operators in the slice are the children of the root operator.
    /// </summary>
    public struct StructExpression
    {
        public JaggedIndexing operationDataSlice;

        public bool IsValid => operationDataSlice.length > 0;

        public static float EvaluateExpression(
            StructExpression structExpression,
            NativeArray<float> inputParameters,
            JaggedIndexing parameterSampleSpace,
            NativeArray<OperatorDefinition> operatorData)
        {
#if RUST_SUBSYSTEM
            return NativeExpressionEvaluator.EvaluateExpression(
                operatorData,
                structExpression.operationDataSlice,
                inputParameters,
                parameterSampleSpace
            );
#else
            var evaler = new DynamicExpressionEvaluator
            {
                expression = structExpression,
                inputParameters0 = inputParameters,
                parameterSampleSpace0 = parameterSampleSpace,
                inputParameters1 = default,
                parameterSampleSpace1 = default,
                operatorData = operatorData
            };
            return evaler.InternalEval(0);
#endif
        }

        public static float EvaluateExpression(
            StructExpression structExpression,
            NativeArray<float> inputParameters0,
            JaggedIndexing parameterSampleSpace0,
            NativeArray<float> inputParameters1,
            JaggedIndexing parameterSampleSpace1,
            NativeArray<OperatorDefinition> operatorData)
        {
#if RUST_SUBSYSTEM
            return NativeExpressionEvaluator.EvaluateExpression(
                operatorData,
                structExpression.operationDataSlice,
                inputParameters0,
                parameterSampleSpace0,
                inputParameters1,
                parameterSampleSpace1
                );
#else
            var evaler = new DynamicExpressionEvaluator
            {
                expression = structExpression,
                inputParameters0 = inputParameters0,
                parameterSampleSpace0 = parameterSampleSpace0,
                inputParameters1 = inputParameters1,
                parameterSampleSpace1 = parameterSampleSpace1,
                operatorData = operatorData
            };
            return evaler.InternalEval(0);
#endif
        }

        private struct DynamicExpressionEvaluator
        {
            public StructExpression expression;
            public NativeArray<float> inputParameters0;
            public JaggedIndexing parameterSampleSpace0;
            public NativeArray<float> inputParameters1;
            public JaggedIndexing parameterSampleSpace1;
            public NativeArray<OperatorDefinition> operatorData;

            public float InternalEval(int operatorToEvaluate)
            {
                var actualOp = operatorData[operatorToEvaluate + expression.operationDataSlice.index];
                switch (actualOp.operator_type)
                {
                    case OperatorType.ConstantValue:
                        return actualOp.node_value;
                    case OperatorType.ParameterValue:
                        var paramIndex = actualOp.parameter_index;
                        if (paramIndex >= parameterSampleSpace0.length)
                        {
                            paramIndex -= parameterSampleSpace0.length;
                            return inputParameters1[paramIndex + parameterSampleSpace1.index];
                        }

                        return inputParameters0[paramIndex + parameterSampleSpace0.index];
                    case OperatorType.Multiply:
                        return InternalEval(actualOp.lhs) * InternalEval(actualOp.rhs);
                    case OperatorType.Divide:
                        return InternalEval(actualOp.lhs) / InternalEval(actualOp.rhs);
                    case OperatorType.Add:
                        return InternalEval(actualOp.lhs) + InternalEval(actualOp.rhs);
                    case OperatorType.Subtract:
                        return InternalEval(actualOp.lhs) - InternalEval(actualOp.rhs);
                    case OperatorType.Remainder:
                        return InternalEval(actualOp.lhs) % InternalEval(actualOp.rhs);
                    case OperatorType.Exponent:
                        return Mathf.Pow(InternalEval(actualOp.lhs), InternalEval(actualOp.rhs));
                    case OperatorType.GreaterThan:
                        return InternalEval(actualOp.lhs) > InternalEval(actualOp.rhs) ? 1 : 0;
                    case OperatorType.LessThan:
                        return InternalEval(actualOp.lhs) < InternalEval(actualOp.rhs) ? 1 : 0;
                    case OperatorType.GreaterThanOrEq:
                        return InternalEval(actualOp.lhs) >= InternalEval(actualOp.rhs) ? 1 : 0;
                    case OperatorType.LessThanOrEq:
                        return InternalEval(actualOp.lhs) <= InternalEval(actualOp.rhs) ? 1 : 0;
                    case OperatorType.Equal:
                        return InternalEval(actualOp.lhs) == InternalEval(actualOp.rhs) ? 1 : 0;
                    case OperatorType.NotEqual:
                        return InternalEval(actualOp.lhs) != InternalEval(actualOp.rhs) ? 1 : 0;
                    case OperatorType.BooleanAnd:
                        return ((InternalEval(actualOp.lhs) > 0.1f) && (InternalEval(actualOp.rhs) > 0.1f)) ? 1 : 0;
                    case OperatorType.BooleanOr:
                        return InternalEval(actualOp.lhs) + InternalEval(actualOp.rhs);

                    case OperatorType.BooleanNot:
                        return (InternalEval(actualOp.rhs) > 0.1f) ? 0 : 1;
                    case OperatorType.NegateUnary:
                        return -InternalEval(actualOp.rhs);
                    default:
                        break;
                }

                return -1f;
            }
        }
    }
}