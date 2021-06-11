using Dman.LSystem.SystemRuntime.Turtle;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleBendTowardsOperation", menuName = "LSystem/TurtleBendOperation")]
    public class TurtleBendTowardsOperation : TurtleOperationSet
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
                    bendTowardsOperation = new TurtleBendTowardsOperationNEW
                    {
                        defaultBendDirection = defaultBendDirection.normalized,
                        defaultBendFactor = defaultBendFactor
                    }
                }
            });
        }
    }

}
