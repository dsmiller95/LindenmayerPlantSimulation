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
    }
}