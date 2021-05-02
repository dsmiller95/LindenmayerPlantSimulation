using Dman.LSystem.SystemRuntime.NativeCollections;
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
        public override IEnumerable<KeyValuePair<int, TurtleOperation>> GetOperators(NativeArrayBuilder<TurtleEntityPrototypeOrganTemplate> organWriter)
        {
            yield return new KeyValuePair<int, TurtleOperation>(bendTowardsOperator, new TurtleOperation
            {
                operationType = TurtleOperationType.BEND_TOWARDS,
                bendTowardsOperation = new TurtleBendTowardsOperationNEW
                {
                    defaultBendDirection = defaultBendDirection.normalized,
                    defaultBendFactor = defaultBendFactor
                }
            });
        }
    }

}
