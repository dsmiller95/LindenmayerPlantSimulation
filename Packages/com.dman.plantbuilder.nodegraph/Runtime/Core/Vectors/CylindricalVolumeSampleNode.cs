using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [System.Serializable, NodeMenuItem("Vectors/CylinderVolumeSample")]
    public class CylindricalVolumeSampleNode : BaseNode
    {
        [Input(name = "Y Range"), SerializeField]
        public Vector2 yRange = new Vector2(0, 1);
        [Input(name = "Arc Percentages"), SerializeField]
        public Vector2 arcRange = new Vector2(0, 1);
        [Input(name = "Magnitute Range"), SerializeField]
        public Vector2 magnituteRange = new Vector2(0, 1);
        [Output(name = "Out")]
        public CylinderCoordinate output;

        public override string name => "Cylinder Volume Sample";

        protected override void Process()
        {
            var rnd = (graph as PlantMeshGeneratorGraph).MyRandom;
            output = new CylinderCoordinate
            {
                y = Mathf.Lerp(yRange.x, yRange.y, (float)rnd.NextDouble()),
                azimuth = Mathf.Lerp(arcRange.x, arcRange.y, (float)rnd.NextDouble()) * Mathf.PI * 2,
                axialDistance = Mathf.Lerp(magnituteRange.x, magnituteRange.y, (float)rnd.NextDouble())
            };
        }
    }
}