using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using System.Collections.Generic;
using Unity.Collections;
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
        public override IEnumerable<KeyValuePair<int, TurtleOperation>> GetOperators(NativeArrayBuilder<TurtleEntityPrototypeOrganTemplate> organWriter)
        {

            yield return new KeyValuePair<int, TurtleOperation>(rollRight, new TurtleOperation
            {
                operationType = TurtleOperationType.ROTATE,
                rotationOperation = new TurtleRotationOperation
                {
                    unitEulerRotation = Vector3.right,
                    defaultTheta = defaultRollTheta
                }
            });
            yield return new KeyValuePair<int, TurtleOperation>(rollLeft, new TurtleOperation
            {
                operationType = TurtleOperationType.ROTATE,
                rotationOperation = new TurtleRotationOperation
                {
                    unitEulerRotation = Vector3.left,
                    defaultTheta = defaultRollTheta
                }
            });


            yield return new KeyValuePair<int, TurtleOperation>(turnRight, new TurtleOperation
            {
                operationType = TurtleOperationType.ROTATE,
                rotationOperation = new TurtleRotationOperation
                {
                    unitEulerRotation = Vector3.down,
                    defaultTheta = defaultRollTheta
                }
            });
            yield return new KeyValuePair<int, TurtleOperation>(turnLeft, new TurtleOperation
            {
                operationType = TurtleOperationType.ROTATE,
                rotationOperation = new TurtleRotationOperation
                {
                    unitEulerRotation = Vector3.up,
                    defaultTheta = defaultRollTheta
                }
            });


            yield return new KeyValuePair<int, TurtleOperation>(tiltUp, new TurtleOperation
            {
                operationType = TurtleOperationType.ROTATE,
                rotationOperation = new TurtleRotationOperation
                {
                    unitEulerRotation = Vector3.forward,
                    defaultTheta = defaultRollTheta
                }
            });
            yield return new KeyValuePair<int, TurtleOperation>(tiltDown, new TurtleOperation
            {
                operationType = TurtleOperationType.ROTATE,
                rotationOperation = new TurtleRotationOperation
                {
                    unitEulerRotation = Vector3.back,
                    defaultTheta = defaultRollTheta
                }
            });
        }
    }

}
