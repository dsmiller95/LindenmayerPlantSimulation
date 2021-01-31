using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    [CreateAssetMenu(fileName = "TurtleScaleOperations", menuName = "LSystem/TurtleScaleOperations")]
    public class TurtleScaleOperations : TurtleOperationSet<TurtleState>
    {
        public char scaleOperator = '!';
        public float defaultScaleAmount = 0.9f;
        public override IEnumerable<ITurtleOperator<TurtleState>> GetOperators()
        {
            yield return new TurtleScaleOperator(scaleOperator, Vector3.one, defaultScaleAmount);
        }

        class TurtleScaleOperator : ITurtleOperator<TurtleState>
        {
            private Vector3 scale;
            private float defaultScale;
            public char TargetSymbol { get; private set; }
            public TurtleScaleOperator(char symbol, Vector3 scale, float defaultScale)
            {
                TargetSymbol = symbol;
                this.scale = scale;
                this.defaultScale = defaultScale;
            }

            public TurtleState Operate(TurtleState initialState, double[] parameters, MeshDraft targetDraft)
            {
                if (parameters.Length != 1 || !(parameters[0] is double scaleAmount))
                {
                    scaleAmount = defaultScale;
                }
                initialState.transformation *= Matrix4x4.Scale((float)scaleAmount * scale);
                return initialState;
            }
        }
    }

}
