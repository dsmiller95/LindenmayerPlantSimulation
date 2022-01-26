using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [System.Serializable, NodeMenuItem("Vectors/Split")]
    public class VectorSplitNode : BaseNode
    {
        [Input(name = "Vector"), SerializeField]
        public Vector3 vector;
        [Output(name = "X")]
        public float x = 0;
        [Output(name = "Y")]
        public float y = 0;
        [Output(name = "Z")]
        public float z = 0;

        public override string name => "Vect Split";

        protected override void Process()
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
    }
}