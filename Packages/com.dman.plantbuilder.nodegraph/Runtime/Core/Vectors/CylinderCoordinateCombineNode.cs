using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [System.Serializable, NodeMenuItem("Vectors/CylinderCombine")]
    public class CylinderCoordinateCombineNode : BaseNode
    {
        [Input(name = "Y"), SerializeField]
        public float y = 0;
        [Input(name = "Angle"), SerializeField]
        public float angle = 0;
        [Input(name = "Magnitute"), SerializeField]
        public float dist = 0;
        [Output(name = "Out")]
        public CylinderCoordinate output;

        public override string name => "Cylinder Coord Combine";

        protected override void Process()
        {
            output = new CylinderCoordinate
            {
                y = y,
                azimuth = angle,
                axialDistance = dist
            };
        }
    }
}