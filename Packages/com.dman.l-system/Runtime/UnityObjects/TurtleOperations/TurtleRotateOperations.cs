using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleRotateOperations", menuName = "LSystem/TurtleRotateOperations")]
    public class TurtleRotateOperations : TurtleOperationSet
    {
        public char rollLeft = '/';
        public char rollRight = '\\';
        public float defaultRollTheta = 18;

        public char turnLeft = '-';
        public char turnRight = '+';
        public float defaultTurnTheta = 18;

        public char tiltUp = '^';
        public char tiltDown = '&';
        public float defaultTiltTheta = 18;
        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {

            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = rollRight,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ROTATE,
                    rotationOperation = new TurtleRotationOperation
                    {
                        unitEulerRotation = Vector3.right,
                        defaultTheta = defaultRollTheta
                    }
                }
            });
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = rollLeft,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ROTATE,
                    rotationOperation = new TurtleRotationOperation
                    {
                        unitEulerRotation = Vector3.left,
                        defaultTheta = defaultRollTheta
                    }
                }
            });


            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = turnRight,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ROTATE,
                    rotationOperation = new TurtleRotationOperation
                    {
                        unitEulerRotation = Vector3.down,
                        defaultTheta = defaultRollTheta
                    }
                }
            });
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = turnLeft,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ROTATE,
                    rotationOperation = new TurtleRotationOperation
                    {
                        unitEulerRotation = Vector3.up,
                        defaultTheta = defaultRollTheta
                    }
                }
            });


            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = tiltUp,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ROTATE,
                    rotationOperation = new TurtleRotationOperation
                    {
                        unitEulerRotation = Vector3.forward,
                        defaultTheta = defaultRollTheta
                    }
                }
            });
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = tiltDown,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ROTATE,
                    rotationOperation = new TurtleRotationOperation
                    {
                        unitEulerRotation = Vector3.back,
                        defaultTheta = defaultRollTheta
                    }
                }
            });
        }
    }
    public struct TurtleRotationOperation
    {
        public float3 unitEulerRotation;
        public float defaultTheta;
        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString)
        {
            var paramIndex = sourceString.parameters[indexInString];
            float theta = defaultTheta;
            if (paramIndex.length == 1)
            {
                theta = sourceString.parameters[paramIndex, 0];
            }
            state.transformation *= Matrix4x4.Rotate(Quaternion.Euler(theta * unitEulerRotation));
        }
    }
}
