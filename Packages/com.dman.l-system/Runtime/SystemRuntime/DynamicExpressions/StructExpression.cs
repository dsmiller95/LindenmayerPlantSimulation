using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DynamicExpressions
{
    public struct StructExpression
    {
        public JaggedIndexing operationDataSlice;

        public bool IsValid => operationDataSlice.length > 0;

        public float EvaluateExpression(
            NativeArray<float> inputParameters,
            JaggedIndexing parameterSampleSpace,
            NativeArray<OperatorDefinition> operatorData)
        {
            var evaler = new DynamicExpressionEvaluator
            {
                expression = this,
                inputParameters = inputParameters,
                parameterSampleSpace = parameterSampleSpace,
                operatorData = operatorData
            };
            return evaler.InternalEval(0);
        }

        private struct DynamicExpressionEvaluator
        {
            public StructExpression expression;
            public NativeArray<float> inputParameters;
            public JaggedIndexing parameterSampleSpace;
            public NativeArray<OperatorDefinition> operatorData;

            public float InternalEval(int operatorToEvaluate)
            {
                var actualOp = operatorData[operatorToEvaluate + expression.operationDataSlice.index];
                switch (actualOp.operatorType)
                {
                    case OperatorType.CONSTANT_VALUE:
                        return actualOp.nodeValue;
                    case OperatorType.PARAMETER_VALUE:
                        return inputParameters[actualOp.parameterIndex];
                    case OperatorType.MULTIPLY:
                        return InternalEval(actualOp.lhs) * InternalEval(actualOp.rhs);
                    case OperatorType.DIVIDE:
                        return InternalEval(actualOp.lhs) / InternalEval(actualOp.rhs);
                    case OperatorType.ADD:
                        return InternalEval(actualOp.lhs) + InternalEval(actualOp.rhs);
                    case OperatorType.SUBTRACT:
                        return InternalEval(actualOp.lhs) - InternalEval(actualOp.rhs);
                    case OperatorType.REMAINDER:
                        return InternalEval(actualOp.lhs) % InternalEval(actualOp.rhs);
                    case OperatorType.EXPONENT:
                        return Mathf.Pow(InternalEval(actualOp.lhs), InternalEval(actualOp.rhs));
                    case OperatorType.GREATER_THAN:
                        return InternalEval(actualOp.lhs) > InternalEval(actualOp.rhs) ? 10 : 0;
                    case OperatorType.LESS_THAN:
                        return InternalEval(actualOp.lhs) < InternalEval(actualOp.rhs) ? 10 : 0;
                    case OperatorType.GREATER_THAN_OR_EQ:
                        return InternalEval(actualOp.lhs) >= InternalEval(actualOp.rhs) ? 10 : 0;
                    case OperatorType.LESS_THAN_OR_EQ:
                        return InternalEval(actualOp.lhs) <= InternalEval(actualOp.rhs) ? 10 : 0;
                    case OperatorType.EQUAL:
                        return InternalEval(actualOp.lhs) == InternalEval(actualOp.rhs) ? 10 : 0;
                    case OperatorType.NOT_EQUAL:
                        return InternalEval(actualOp.lhs) != InternalEval(actualOp.rhs) ? 10 : 0;
                    case OperatorType.BOOLEAN_AND:
                        return ((InternalEval(actualOp.lhs) > 0.1f) && (InternalEval(actualOp.rhs) > 0.1f)) ? 10 : 0;
                    case OperatorType.BOOLEAN_OR:
                        return InternalEval(actualOp.lhs) + InternalEval(actualOp.rhs);

                    case OperatorType.BOOLEAN_NOT:
                        return (InternalEval(actualOp.rhs) > 0.1f) ? 10 : 0;
                    case OperatorType.NEGATE_UNARY:
                        return -InternalEval(actualOp.rhs);
                    default:
                        break;
                }
                return -1f;
            }
        }
    }

}
