using Dman.Utilities.SerializableUnityObjects;
using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using PlantBuilder.NodeGraph.MeshNodes;
using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("SplineNode")]
    public class SplineNodeNode : BaseNode
    {
        [Input(name = "Position"), SerializeField]
        public Vector3 positionInput;
        [Input(name = "Direction"), SerializeField]
        public Vector3 direction;
        [Input(name = "Up"), SerializeField]
        public Vector3 up = Vector3.up;
        [Input(name = "Scale"), SerializeField]
        public Vector2 scale = Vector3.one;
        [Input(name = "Roll"), SerializeField]
        public float roll;

        [Output(name = "Out")]
        public SplineNode output;

        public override string name => "SplineNode";

        protected override void Process()
        {
            output = new SplineNode(positionInput, direction)
                {
                    Up = up,
                    Scale = scale,
                    Roll = roll
                };
        }

        //[System.Serializable]
        //class DefferedSplineMeshWrapper : DeferredEvaluator<SplineNode>
        //{
        //    public SerializableVector3 positionInput;
        //    public SerializableVector3 direction;
        //    public SerializableVector3 up;
        //    public SerializableVector2 scale;
        //    public float roll;

        //    public DefferedSplineMeshWrapper(SplineNodeNode node)
        //    {
        //        positionInput = node.positionInput;
        //        direction = node.direction;
        //        up = node.up;
        //        scale = node.scale;
        //        roll = node.roll;
        //    }

        //    public override SplineNode Evalute(System.Random randomSource, Dictionary<string, object> context)
        //    {
        //        return new SplineNode(positionInput, direction)
        //        {
        //            Up = up,
        //            Scale = scale,
        //            Roll = roll
        //        };
        //    }
        //}
    }
}