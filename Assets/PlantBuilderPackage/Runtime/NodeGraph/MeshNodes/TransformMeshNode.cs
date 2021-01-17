using Dman.Utilities.SerializableUnityObjects;
using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Transform")] // Add the node in the node creation context menu
    public class TransformMeshNode : BaseNode
    {
        [Input(name = "Mesh")]
        public DeferredEvaluator<MeshDraftWithExtras> mesh;
        [Input(name = "Translation")]
        public DeferredEvaluator<Vector3> translation = Vector3.zero;
        [Input(name = "Rotation")]
        public DeferredEvaluator<Vector3> eulerRotation = Vector3.zero;
        [Input(name = "Scale")]
        public DeferredEvaluator<Vector3> scale = Vector3.one;

        [Output(name = "Out")]
        public DeferredEvaluator<MeshDraftWithExtras> output;

        public override string name => "Transform mesh";

        protected override void Process()
        {
            output = new TransformMesh(this);
        }

        [System.Serializable]
        class TransformMesh : DeferredEvaluator<MeshDraftWithExtras>
        {
            private DeferredEvaluator<MeshDraftWithExtras> meshGen;
            private DeferredEvaluator<Vector3> translate;
            private DeferredEvaluator<Vector3> eulerRotation;
            private DeferredEvaluator<Vector3> scale;

            public TransformMesh(TransformMeshNode node)
            {
                this.meshGen = node.mesh;
                this.translate = node.translation;
                this.eulerRotation = node.eulerRotation;
                this.scale = node.scale;
            }

            public override MeshDraftWithExtras Evalute(System.Random randomSource, Dictionary<string, object> context)
            {
                var mesh = meshGen.Evalute(randomSource, context);
                var translateVal = translate.Evalute(randomSource, context);
                var eulerRotationVal = eulerRotation.Evalute(randomSource, context);
                var scaleVal = scale.Evalute(randomSource, context);

                return mesh.Transform(Matrix4x4.TRS(translateVal, Quaternion.Euler(eulerRotationVal), scaleVal));
            }
        }
    }
}