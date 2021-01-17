using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [System.Serializable, NodeMenuItem("Vectors/Combine")]
    public class VectorCombineNode : BaseNode
    {
        [Input(name = "X"), SerializeField]
        public float x = 0;
        [Input(name = "Y"), SerializeField]
        public float y = 0;
        [Input(name = "Z"), SerializeField]
        public float z = 0;
        [Output(name = "Out")]
        public Vector3 output;

        public override string name => "Vect Combine";

        protected override void Process()
        {
            output = new Vector3(x, y, z);
        }
    }
}