using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.Extern.Adapters
{
    public static class NativeExpressionEvaluator
    {
        public static float EvaluateExpression(
            NativeArray<OperatorDefinition> operatorData, 
            JaggedIndexing operatorSpace,
            NativeArray<float> inputParameters,
            JaggedIndexing parameterSampleSpace,
            NativeArray<float> inputParameters2,
            JaggedIndexing parameterSampleSpace2)
        {
            unsafe
            {
                var operationPointer = operatorData.GetUnsafeReadOnlyPtr();
                var parameterPointer = inputParameters.GetUnsafeReadOnlyPtr();
                var parameterPointer2 = inputParameters2.GetUnsafeReadOnlyPtr();
                
                return SystemRuntimeRust.evaluate_expression(
                    (OperatorDefinition*)operationPointer,
                    &operatorSpace,
                    (float*)parameterPointer,
                    &parameterSampleSpace,
                    (float*)parameterPointer2,
                    &parameterSampleSpace2
                    );
            }
        }
        public static float EvaluateExpression(
            NativeArray<OperatorDefinition> operatorData, 
            JaggedIndexing operatorSpace,
            NativeArray<float> inputParameters,
            JaggedIndexing parameterSampleSpace)
        {
            unsafe
            {
                var operationPointer = operatorData.GetUnsafeReadOnlyPtr();
                var parameterPointer = inputParameters.GetUnsafeReadOnlyPtr();

                JaggedIndexing noLengthIndex = JaggedIndexing.GetWithNoLength(parameterSampleSpace.index); 
                
                return SystemRuntimeRust.evaluate_expression(
                    (OperatorDefinition*)operationPointer,
                    &operatorSpace,
                    (float*)parameterPointer,
                    &parameterSampleSpace,
                    (float*)parameterPointer,
                    &noLengthIndex
                );
            }
        }
    }
}