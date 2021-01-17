using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Source/Capsule")] // Add the node in the node creation context menu
    public class CapsuleMeshNode : MeshNode
    {
        [Input(name = "Height"), SerializeField]
        public float height = 1;
        [Input(name = "Radius"), SerializeField]
        public float radius = 1;

        public override string name => "Capsule";

        protected override void Process()
        {
            output = new MeshDraftWithExtras(
                    MeshDraft.Capsule(height, radius),
                    new Bounds(Vector3.zero, new Vector3(radius * 2, height, radius * 2)));
        }

        //[System.Serializable]
        //class DeferredCapsuleBuilder : DeferredEvaluator<MeshDraftWithExtras>
        //{
        //    private DeferredEvaluator<float> height;
        //    private DeferredEvaluator<float> radius;

        //    public DeferredCapsuleBuilder(CapsuleMeshNode node)
        //    {
        //        height = node.height;
        //        radius = node.radius;
        //    }

        //    public override MeshDraftWithExtras Evalute(System.Random randomSource, Dictionary<string, object> context)
        //    {
        //        var heightNum = height.Evalute(randomSource, context);
        //        var radiusNum = radius.Evalute(randomSource, context);
        //        return new MeshDraftWithExtras(
        //            MeshDraft.Capsule(heightNum, radiusNum),
        //            new Bounds(Vector3.zero, new Vector3(radiusNum * 2, radiusNum * 2, heightNum)));
        //    }
        //}
    }
}