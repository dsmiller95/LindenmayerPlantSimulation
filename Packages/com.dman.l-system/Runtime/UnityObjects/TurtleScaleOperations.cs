using Dman.LSystem.SystemRuntime;
using System.Collections.Generic;
using Unity.Collections;
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

            public TurtleState Operate(
                TurtleState initialState,
                NativeArray<float> parameters,
                SymbolString<float>.JaggedIndexing parameterIndexing,
                TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate> targetDraft)
            {
                var p0 = parameterIndexing.Start;
                if (parameterIndexing.length == 0)
                {
                    initialState.thickness *= defaultScale;
                }
                else
                if (parameterIndexing.length == 1)
                {
                    initialState.thickness *= parameters[p0 + 0];
                }
                else
                {
                    Debug.LogError($"Invalid scale parameter length: {parameterIndexing.length}");
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

            public TurtleState Operate(
                TurtleState initialState,
                NativeArray<float> parameters,
                SymbolString<float>.JaggedIndexing parameterIndexing,
                TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate> targetDraft)
            {
                var p0 = parameterIndexing.Start;
                if (parameterIndexing.length== 0)
                {
                    initialState.transformation *= Matrix4x4.Scale(defaultScale * scale);
                }
                else
                if (parameterIndexing.length == 1)
                {
                    initialState.transformation *= Matrix4x4.Scale(parameters[p0 + 0] * scale);
                }
                else
                if (parameterIndexing.length == 3)
                {
                    initialState.transformation *= Matrix4x4.Scale(new Vector3(parameters[p0 + 0], parameters[p0 + 1], parameters[p0 + 2]));
                }
                else
                {
                    Debug.LogError($"Invalid scale parameter length: {parameterIndexing.length}");
                }
                return initialState;
            }
        }
    }

}
