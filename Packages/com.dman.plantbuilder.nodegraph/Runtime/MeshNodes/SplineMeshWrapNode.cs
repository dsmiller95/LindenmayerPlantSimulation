using Dman.MeshDraftExtensions;
using GraphProcessor;
using SplineMesh;
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
    }
}