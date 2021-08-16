using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleBendTowardsOperation", menuName = "LSystem/TurtleBendOperation")]
    public class TurtleBendTowardsOperationSet : TurtleOperationSet
    {
        [Header("$(t, x, y, z): Bend towards the vector <x, y, z> by theta T")]
        [Header("$(x, y, z): Bend towards the vector <x, y, z> by the default theta")]
        [Header("$(t): Bend towards the default direction by t theta")]
        [Header("$: Bend towards the default direction by the default theta")]
        public char bendTowardsOperator = '$';
        public Vector3 defaultBendDirection = Vector3.down;
        [Range(0, 1)]
        public float defaultBendFactor = 0.1f;
        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = bendTowardsOperator,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.BEND_TOWARDS,
                    bendTowardsOperation = new TurtleBendTowardsOperation
                    {
                        defaultBendDirection = defaultBendDirection.normalized,
                        defaultBendFactor = defaultBendFactor
                    }
                }
            });
        }
    }
    public struct TurtleBendTowardsOperation
    {
        public float3 defaultBendDirection;
        public float defaultBendFactor;
        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString)
        {
            var paramIndex = sourceString.parameters[indexInString];
            float bendFactor = defaultBendFactor;
            if (paramIndex.length == 1)
            {
                bendFactor = sourceString.parameters[paramIndex, 0];
            }
            var localBendDirection = state.transformation.inverse.MultiplyVector(defaultBendDirection);
            var adjustment = (bendFactor) * (Vector3.Cross(localBendDirection, Vector3.right));
            state.transformation *= Matrix4x4.Rotate(
                Quaternion.Slerp(
                    Quaternion.identity,
                    Quaternion.FromToRotation(
                        Vector3.right,
                        localBendDirection),
                    adjustment.magnitude
                )
            );
        }
    }

}
