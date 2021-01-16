using GraphProcessor;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Source/Capsule")] // Add the node in the node creation context menu
    public class CapsuleMeshNode : BaseNode
    {
        [Input(name = "Height")]
        public float height = 1;
        [Input(name = "Radius")]
        public float radius = 1;

        [Output(name = "Out")]
        public DeferredMeshEvaluator output;

        public override string name => "Capsule";

        protected override void Process()
        {
            Debug.Log("evaluaged capsule");
            output = new DeferredCapsuleBuilder(this);
        }

        [System.Serializable]
        class DeferredCapsuleBuilder : DeferredMeshEvaluator
        {
            private float height;
            private float radius;

            public DeferredCapsuleBuilder(CapsuleMeshNode node)
            {
                height = node.height;
                radius = node.radius;
            }

            public override PlantMeshComponent Evalute(Dictionary<string, object> context)
            {
                return new PlantMeshComponent
                {
                    meshDraft = MeshDraft.Capsule(height, radius)
                };
            }
        }
    }
}