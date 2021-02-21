using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Vectors/Math")]
    public class BinaryVectorMathOperationNode : BaseNode
    {
        [Input(name = "A"), SerializeField]
        public Vector3 a;
        [Input(name = "B"), SerializeField]
        public Vector3 b;
        [SerializeField]
        public OperationType operation;

        [Output(name = "Out")]
        public Vector3 output;

        public override string name => "Vector Math";

        public enum OperationType
        {
            ADD,
            SCALE,
            SUBTRACT,
        }

        protected override void Process()
        {
            switch (operation)
            {
                case OperationType.ADD:
                    output = a + b;
                    break;
                case OperationType.SCALE:
                    output = Vector3.Scale(a, b);
                    break;
                case OperationType.SUBTRACT:
                    output = a - b;
                    break;
                default:
                    break;
            }
        }
    }
}