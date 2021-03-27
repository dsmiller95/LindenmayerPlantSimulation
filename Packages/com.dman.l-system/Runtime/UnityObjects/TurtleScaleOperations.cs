using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleScaleOperations", menuName = "LSystem/TurtleScaleOperations")]
    public class TurtleScaleOperations : TurtleOperationSet<TurtleState>
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


        public override IEnumerable<ITurtleOperator<TurtleState>> GetOperators()
        {
            yield return new TurtleScaleOperator(scaleOperator, Vector3.one, defaultScaleAmount);
            yield return new TurtleThicknessScaleOperator(thicknessScaleOperator, defaultThicknessScale);
        }

        class TurtleThicknessScaleOperator : ITurtleOperator<TurtleState>
        {
            private float defaultScale;
            public char TargetSymbol { get; private set; }
            public TurtleThicknessScaleOperator(char symbol, float defaultScale)
            {
                TargetSymbol = symbol;
                this.defaultScale = defaultScale;
            }

            public TurtleState Operate(TurtleState initialState, double[] parameters, MeshDraft targetDraft)
            {
                if (parameters.Length == 0)
                {
                    initialState.thickness *= defaultScale;
                }
                else
                if (parameters.Length == 1)
                {
                    initialState.thickness *= (float)parameters[0];
                }
                else
                {
                    Debug.LogError($"Invalid scale parameter length: {parameters.Length}");
                }
                return initialState;
            }
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
                if (parameters.Length == 0)
                {
                    initialState.transformation *= Matrix4x4.Scale((float)defaultScale * scale);
                }
                else
                if (parameters.Length == 1)
                {
                    initialState.transformation *= Matrix4x4.Scale((float)parameters[0] * scale);
                }
                else
                if (parameters.Length == 3)
                {
                    initialState.transformation *= Matrix4x4.Scale(new Vector3((float)parameters[0], (float)parameters[1], (float) parameters[2]));
                }else
                {
                    Debug.LogError($"Invalid scale parameter length: {parameters.Length}");
                }
                return initialState;
            }
        }
    }

}
