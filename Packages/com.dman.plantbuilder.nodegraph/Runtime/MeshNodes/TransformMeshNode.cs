using Dman.MeshDraftExtensions;
using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Transform")] // Add the node in the node creation context menu
    public class TransformMeshNode : MeshNode
    {
        [Input(name = "Mesh")]
        public MeshDraftWithExtras mesh;
        [Input(name = "Translation"), SerializeField]
        public Vector3 translation = Vector3.zero;
        [Input(name = "Rotation"), SerializeField]
        public Vector3 eulerRotation = Vector3.zero;
        [Input(name = "Scale"), SerializeField]
        public Vector3 scale = Vector3.one;

        public override string name => "Transform mesh";

        protected override void Process()
        {
            output = mesh.Transform(Matrix4x4.TRS(translation, Quaternion.Euler(eulerRotation), scale));
        }

        //[System.Serializable]
        //class TransformMesh : DeferredEvaluator<MeshDraftWithExtras>
        //{
        //    private DeferredEvaluator<MeshDraftWithExtras> meshGen;
        //    private DeferredEvaluator<Vector3> translate;
        //    private DeferredEvaluator<Vector3> eulerRotation;
        //    private DeferredEvaluator<Vector3> scale;

        //    public TransformMesh(TransformMeshNode node)
        //    {
        //        this.meshGen = node.mesh;
        //        this.translate = node.translation;
        //        this.eulerRotation = node.eulerRotation;
        //        this.scale = node.scale;
        //    }

        //    public override MeshDraftWithExtras Evalute(System.Random randomSource, Dictionary<string, object> context)
        //    {
        //        var mesh = meshGen.Evalute(randomSource, context);
        //        var translateVal = translate.Evalute(randomSource, context);
        //        var eulerRotationVal = eulerRotation.Evalute(randomSource, context);
        //        var scaleVal = scale.Evalute(randomSource, context);

        //        return mesh.Transform(Matrix4x4.TRS(translateVal, Quaternion.Euler(eulerRotationVal), scaleVal));
        //    }
        //}
    }
}