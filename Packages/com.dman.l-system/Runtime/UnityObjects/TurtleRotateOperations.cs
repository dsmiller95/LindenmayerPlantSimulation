using Dman.LSystem.SystemRuntime;
using ProceduralToolkit;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleRotateOperations", menuName = "LSystem/TurtleRotateOperations")]
    public class TurtleRotateOperations : TurtleOperationSet<TurtleState>
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
        public override IEnumerable<ITurtleOperator<TurtleState>> GetOperators()
        {
            yield return new TurtleRotateOperator(Vector3.right, rollRight, defaultRollTheta);
            yield return new TurtleRotateOperator(Vector3.left, rollLeft, defaultRollTheta);

            yield return new TurtleRotateOperator(Vector3.down, turnRight, defaultTurnTheta);
            yield return new TurtleRotateOperator(Vector3.up, turnLeft, defaultTurnTheta);

            yield return new TurtleRotateOperator(Vector3.forward, tiltUp, defaultTiltTheta);
            yield return new TurtleRotateOperator(Vector3.back, tiltDown, defaultTiltTheta);
        }

        class TurtleRotateOperator : ITurtleOperator<TurtleState>
        {
            private Vector3 unitEulerRotation;
            private float defaultTheta;
            public char TargetSymbol { get; private set; }
            public TurtleRotateOperator(Vector3 euler, char symbol, float defaultTheta)
            {
                TargetSymbol = symbol;
                unitEulerRotation = euler;
                this.defaultTheta = defaultTheta;
            }

            public TurtleState Operate(
                TurtleState initialState,
                NativeArray<float> parameters,
                SymbolString<float>.JaggedIndexing parameterIndexing,
                TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate> targetDraft)
            {
                var p0 = parameterIndexing.Start;
                float theta = defaultTheta;
                if (parameterIndexing.length == 1)
                {
                    theta = parameters[p0 + 0];
                }
                initialState.transformation *= Matrix4x4.Rotate(Quaternion.Euler(theta * unitEulerRotation));
                return initialState;
            }
        }
    }

}
