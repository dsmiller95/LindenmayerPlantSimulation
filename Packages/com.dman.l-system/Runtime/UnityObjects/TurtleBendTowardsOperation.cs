using Dman.LSystem.SystemRuntime;
using ProceduralToolkit;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleBendTowardsOperation", menuName = "LSystem/TurtleBendOperation")]
    public class TurtleBendTowardsOperation : TurtleOperationSet<TurtleState>
    {
        [Header("$(t, x, y, z): Bend towards the vector <x, y, z> by theta T")]
        [Header("$(x, y, z): Bend towards the vector <x, y, z> by the default theta")]
        [Header("$(t): Bend towards the default direction by t theta")]
        [Header("$: Bend towards the default direction by the default theta")]
        public char bendTowardsOperator = '$';
        public Vector3 defaultBendDirection = Vector3.down;
        [Range(0, 1)]
        public float defaultBendFactor = 0.1f;
        public override IEnumerable<ITurtleOperator<TurtleState>> GetOperators()
        {
            yield return new BendTowardsOperator(defaultBendDirection.normalized, bendTowardsOperator, defaultBendFactor);
        }

        class BendTowardsOperator : ITurtleOperator<TurtleState>
        {
            private Vector3 worldBendDirection;
            private float defaultBendFactor;
            public char TargetSymbol { get; private set; }
            public BendTowardsOperator(Vector3 lookdir, char symbol, float defaultTheta)
            {
                TargetSymbol = symbol;
                worldBendDirection = lookdir;
                this.defaultBendFactor = defaultTheta;
            }

            public TurtleState Operate(
                TurtleState initialState,
                NativeArray<float> parameters,
                JaggedIndexing parameterIndexing,
                TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate> targetDraft)
            {
                var p0 = parameterIndexing.Start;
                float bendFactor = defaultBendFactor;
                if (parameterIndexing.length == 1)
                {
                    bendFactor = parameters[p0];
                }
                var localBendDirection = initialState.transformation.inverse.MultiplyVector(worldBendDirection);
                var adjustment = (bendFactor) * (Vector3.Cross(localBendDirection, Vector3.right));
                initialState.transformation *= Matrix4x4.Rotate(Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.right, localBendDirection), adjustment.magnitude));
                return initialState;
            }
        }
    }

}
