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
                inputParameters0 = inputParameters,
                parameterSampleSpace0 = parameterSampleSpace,
                inputParameters1 = default,
                parameterSampleSpace1 = default,
                operatorData = operatorData
            };
            return evaler.InternalEval(0);
        }
        public float EvaluateExpression(
            NativeArray<float> inputParamSet0,
            JaggedIndexing parameterSampleSpace0,
            NativeArray<float> inputParamSet1,
            JaggedIndexing parameterSampleSpace1,
            NativeArray<OperatorDefinition> operatorData)
        {
            var evaler = new DynamicExpressionEvaluator
            {
                expression = this,
                inputParameters0 = inputParamSet0,
                parameterSampleSpace0 = parameterSampleSpace0,
                inputParameters1 = inputParamSet1,
                parameterSampleSpace1 = parameterSampleSpace1,
                operatorData = operatorData
            };
            return evaler.InternalEval(0);
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
                switch (actualOp.operatorType)
                {
                    case OperatorType.CONSTANT_VALUE:
                        return actualOp.nodeValue;
                    case OperatorType.PARAMETER_VALUE:
                        var paramIndex = actualOp.parameterIndex;
                        if(paramIndex >= parameterSampleSpace0.length)
                        {
                            paramIndex -= parameterSampleSpace0.length;
                            return inputParameters1[paramIndex + parameterSampleSpace1.index];
                        }
                        return inputParameters0[paramIndex + parameterSampleSpace0.index];
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
