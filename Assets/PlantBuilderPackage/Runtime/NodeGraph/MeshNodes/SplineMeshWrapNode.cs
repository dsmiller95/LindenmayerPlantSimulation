using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/SplineWrap")] // Add the node in the node creation context menu
    public class SplineMeshWrapNode : MeshNode
    {
        [Input(name = "Spline")]
        public CubicBezierCurve spline;
        [Input(name = "Mesh")]
        public MeshDraftWithExtras mesh;

        public override string name => "Spline wrap";

        protected override void Process()
        {
            output = mesh.WrapSplineAll(spline);
        }

        //[System.Serializable]
        //class DefferedSplineMeshWrapper : DeferredEvaluator<MeshDraftWithExtras>
        //{
        //    private DeferredEvaluator<CubicBezierCurve> curve;
        //    private DeferredEvaluator<MeshDraftWithExtras> mesh;

        //    public DefferedSplineMeshWrapper(SplineMeshWrapNode node)
        //    {
        //        this.curve = node.spline;
        //        this.mesh = node.mesh;
        //    }

        //    public override MeshDraftWithExtras Evalute(System.Random randomSource, Dictionary<string, object> context)
        //    {
        //        var originMesh = mesh.Evalute(randomSource, context);
        //        var spline = curve.Evalute(randomSource, context);

        //        return originMesh.WrapSplineAll(spline);
        //    }
        //}
    }
}