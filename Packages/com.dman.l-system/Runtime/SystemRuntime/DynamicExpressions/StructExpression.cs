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
            return NativeExpressionEvaluator.EvaluateExpression(
                operatorData,
                structExpression.operationDataSlice,
                inputParameters,
                parameterSampleSpace
            );
        }
        public static float EvaluateExpression(
            StructExpression structExpression,
            NativeArray<float> inputParameters0,
            JaggedIndexing parameterSampleSpace0,
            NativeArray<float> inputParameters1,
            JaggedIndexing parameterSampleSpace1,
            NativeArray<OperatorDefinition> operatorData)
        {
            return NativeExpressionEvaluator.EvaluateExpression(
                operatorData,
                structExpression.operationDataSlice,
                inputParameters0,
                parameterSampleSpace0,
                inputParameters1,
                parameterSampleSpace1
                );
        }
    }

}
