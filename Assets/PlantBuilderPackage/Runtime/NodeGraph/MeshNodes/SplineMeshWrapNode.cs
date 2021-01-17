using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/SplineWrap")] // Add the node in the node creation context menu
    public class SplineMeshWrapNode : BaseNode
    {
        [Input(name = "Spline")]
        public DeferredEvaluator<CubicBezierCurve> spline;
        [Input(name = "Mesh")]
        public DeferredEvaluator<MeshDraftWithExtras> mesh;

        [Output(name = "Out")]
        public DeferredEvaluator<MeshDraftWithExtras> output;

        public override string name => "Spline wrap";

        protected override void Process()
        {
            output = new DefferedSplineMeshWrapper(this);
        }

        [System.Serializable]
        class DefferedSplineMeshWrapper : DeferredEvaluator<MeshDraftWithExtras>
        {
            private DeferredEvaluator<CubicBezierCurve> curve;
            private DeferredEvaluator<MeshDraftWithExtras> mesh;

            public DefferedSplineMeshWrapper(SplineMeshWrapNode node)
            {
                this.curve = node.spline;
                this.mesh = node.mesh;
            }

            public override MeshDraftWithExtras Evalute(System.Random randomSource, Dictionary<string, object> context)
            {
                var originMesh = mesh.Evalute(randomSource, context);
                var spline = curve.Evalute(randomSource, context);

                return originMesh.WrapSplineAll(spline);
            }
        }
    }
}