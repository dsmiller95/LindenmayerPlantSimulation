using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleScaleOperations", menuName = "LSystem/TurtleScaleOperations")]
    public class TurtleScaleOperations : TurtleOperationSet
    {
        [Header("!(x, y, z): Scale by the vector <x, y, z>")]
        [Header("!(x): scale all axises by x")]
        [Header("!: scale by default scale amount")]
        public char scaleOperator = '!';
        public float defaultScaleAmount = 0.9f;


        [Header("@(x): scale Thickness by x")]
        [Header("@: scale Thickness by default amount")]
        public char thicknessScaleOperator = '@';
        public float defaultThicknessScale = 0.9f;


        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = scaleOperator,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.SCALE_TRANSFORM,
                    scaleOperation = new TurtleScaleOperation
                    {
                        nonUniformScale = Vector3.one,
                        defaultScaleFactor = defaultScaleAmount
                    }
                }
            });
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = thicknessScaleOperator,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.SCALE_THICCNESS,
                    thiccnessOperation = new TurtleThiccnessOperation
                    {
                        defaultThicknessScale = defaultThicknessScale
                    }
                }
            });
        }
    }

    public struct TurtleThiccnessOperation
    {
        public float defaultThicknessScale;
        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString)
        {
            var paramIndex = sourceString.parameters[indexInString];

            if (paramIndex.length == 0)
            {
                state.thickness *= defaultThicknessScale;
            }
            else
            if (paramIndex.length == 1)
            {
                state.thickness *= sourceString.parameters[paramIndex, 0];
            }
            else
            {
                Debug.LogError($"Invalid scale parameter length: {paramIndex.length}");
            }
        }
    }
    public struct TurtleScaleOperation
    {
        public float3 nonUniformScale;
        public float defaultScaleFactor;
        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString)
        {
            var paramIndex = sourceString.parameters[indexInString];
            if (paramIndex.length == 0)
            {
                state.transformation *= Matrix4x4.Scale(defaultScaleFactor * nonUniformScale);
            }
            else
            if (paramIndex.length == 1)
            {
                state.transformation *= Matrix4x4.Scale(sourceString.parameters[paramIndex, 0] * nonUniformScale);
            }
            else
            if (paramIndex.length == 3)
            {
                state.transformation *= Matrix4x4.Scale(
                    new Vector3(
                        sourceString.parameters[paramIndex, 0],
                        sourceString.parameters[paramIndex, 1],
                        sourceString.parameters[paramIndex, 2]
                    ));
            }
            else
            {
                Debug.LogError($"Invalid scale parameter length: {paramIndex.length}");
            }
        }
    }

}
