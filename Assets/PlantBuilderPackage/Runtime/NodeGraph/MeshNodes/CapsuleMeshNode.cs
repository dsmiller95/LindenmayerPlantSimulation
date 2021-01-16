using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Source/Capsule")] // Add the node in the node creation context menu
    public class CapsuleMeshNode : BaseNode
    {
        [Input(name = "Height")]
        public DeferredEvaluator<float> height = 1;
        [Input(name = "Radius")]
        public DeferredEvaluator<float> radius = 1;

        [Output(name = "Out"), SerializeField]
        public DeferredEvaluator<PlantMeshComponent> output;

        public override string name => "Capsule";

        protected override void Process()
        {
            output = new DeferredCapsuleBuilder(this);
        }

        [System.Serializable]
        class DeferredCapsuleBuilder : DeferredEvaluator<PlantMeshComponent>
        {
            private DeferredEvaluator<float> height;
            private DeferredEvaluator<float> radius;

            public DeferredCapsuleBuilder(CapsuleMeshNode node)
            {
                height = node.height;
                radius = node.radius;
            }

            public override PlantMeshComponent Evalute(System.Random randomSource, Dictionary<string, object> context)
            {
                var heightNum = height.Evalute(randomSource, context);
                var radiusNum = radius.Evalute(randomSource, context);
                return new PlantMeshComponent
                {
                    meshDraft = MeshDraft.Capsule(heightNum, radiusNum)
                };
            }
        }
    }
}