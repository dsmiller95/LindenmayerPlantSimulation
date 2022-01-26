using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/BinaryMath")] // Add the node in the node creation context menu
    public class BinaryMathOperationNode : BaseNode
    {
        [Input(name = "A"), SerializeField]
        public float a = 0;
        [Input(name = "B"), SerializeField]
        public float b = 0;
        [SerializeField]
        public OperationType operation;

        [Output(name = "Out")]
        public float output;

        public override string name => "Math";

        public enum OperationType
        {
            ADD,
            MULTIPLY,
            SUBTRACT,
            DIVIDE,
        }

        protected override void Process()
        {
            switch (operation)
            {
                case OperationType.ADD:
                    output = a + b;
                    break;
                case OperationType.MULTIPLY:
                    output = a * b;
                    break;
                case OperationType.SUBTRACT:
                    output = a - b;
                    break;
                case OperationType.DIVIDE:
                    output = a / b;
                    break;
                default:
                    break;
            }
        }
    }
}