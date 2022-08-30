using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleOrientTowardsOperation", menuName = "LSystem/TurtleOrientOperation")]
    public class TurtleOrientTowardsOperationSet : TurtleOperationSet
    {
        [Header("Rotates turtle about the X-axis to align y-axis to point towards the orientation direction as much as possible")]
        [Header("*(t, x, y, z): Orient y-axis towards the vector <x, y, z> by factor T")]
        [Header("*(x, y, z): Orient y-axis towards the vector <x, y, z> by the default factor")]
        [Header("*(t): Orient y-axis towards the default direction by factor t")]
        [Header("*: Orient y-axis towards the default direction by the default factor")]
        public char orientTowardsOperator = '*';
        public Vector3 defaultOrientDirection = Vector3.right;
        [Range(0, 1)]
        public float defaultOrientFactor = 0.1f;
        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = orientTowardsOperator,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ORIENT_TOWARDS,
                    orientTowardsOperation = new TurtleOrientTowardsOperation
                    {
                        defaultOrientDirection = defaultOrientDirection.normalized,
                        defaultOrientFactor = defaultOrientFactor
                    }
                }
            });
        }
    }
    public struct TurtleOrientTowardsOperation
    {
        public float3 defaultOrientDirection;
        public float defaultOrientFactor;
        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString)
        {
            var paramIndex = sourceString.parameters[indexInString];
            float bendFactor = defaultOrientFactor;
            var hasOrientFactor = paramIndex.length == 1 || paramIndex.length == 4;
            if (hasOrientFactor)
            {
                bendFactor = sourceString.parameters[paramIndex, 0];
            }
            var orientDirection = defaultOrientDirection;
            if(paramIndex.length == 3 || paramIndex.length == 4)
            {
                var offset = hasOrientFactor ? 1 : 0;
                orientDirection = new float3(
                    sourceString.parameters[paramIndex, offset],
                    sourceString.parameters[paramIndex, offset + 1],
                    sourceString.parameters[paramIndex, offset + 2]);
            }
            var localOrientDirection = state.transformation.inverse.MultiplyVector(orientDirection);
            var looker = Quaternion.LookRotation(Vector3.right, localOrientDirection) * Quaternion.Euler(0, -90, 0);
            state.transformation *= Matrix4x4.Rotate(
                Quaternion.Slerp(
                    Quaternion.identity,
                    looker,
                    bendFactor
                )
            );
        }
    }

}
