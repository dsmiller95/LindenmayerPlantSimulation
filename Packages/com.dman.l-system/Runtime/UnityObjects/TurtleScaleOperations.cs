using Dman.LSystem.SystemRuntime.Turtle;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "TurtleScaleOperations", menuName = "LSystem/TurtleScaleOperations")]
    public class TurtleScaleOperations : TurtleOperationSet
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


        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = scaleOperator,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.SCALE_TRANSFORM,
                    scaleOperation = new TurtleScaleOperation
                    {
                        nonUniformScale = Vector3.one,
                        defaultScaleFactor = defaultScaleAmount
                    }
                }
            });
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = thicknessScaleOperator,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.SCALE_THICCNESS,
                    thiccnessOperation = new TurtleThiccnessOperation
                    {
                        defaultThicknessScale = defaultThicknessScale
                    }
                }
            });
        }
    }

}
