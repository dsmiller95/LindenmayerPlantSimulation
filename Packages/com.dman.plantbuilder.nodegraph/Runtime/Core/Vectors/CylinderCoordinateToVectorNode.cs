using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [System.Serializable, NodeMenuItem("Vectors/CylinderToCartesian")]
    public class CylinderCoordinateToVectorNode : BaseNode
    {
        [Input(name = "Input"), SerializeField]
        public CylinderCoordinate input;
        [Output(name = "Out")]
        public Vector3 output;

        public override string name => "Cylinder To Cartesian";

        protected override void Process()
        {
            output = input.ToCartesian();
        }
    }
}