using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/SplineChildPlacement")]
    public class SplineChildMeshPlacementNode : MeshNode
    {
        [Input(name = "Spline")]
        public CubicBezierCurve spline;
        [Input(name = "Child Mesh")]
        public MeshDraftWithExtras childMesh;

        [Input(name = "Stem radius"), SerializeField]
        public float placementRadius = 1;
        [Input(name = "Children"), SerializeField]
        public int childComponentCount = 10;
        [Input(name = "Degrees per child"), SerializeField]
        public float rotationPerChild = 360 * 3 / 5;
        [Input(name = "First Child"), SerializeField]
        [Range(0, 1f)]
        public float childBeginRange = .3f;
        [Input(name = "Last Child"), SerializeField]
        [Range(0, 1f)]
        public float childEndRange = .7f;
        [Input(name = "Random factor"), SerializeField]
        public float childRandomFactor = .05f;
        [Input(name = "Child size reduction"), SerializeField]
        public float childSizeReduction = 0.2f;

        public override string name => "Spline children";

        protected override void Process()
        {
            var output = new CompoundMeshDraft();
            output.name = childMesh.meshDraft.name;
            var rnd = (graph as PlantMeshGeneratorGraph).MyRandom;
            var childTransforms = SplineTransformPlacement.GenerateTransformationsAlongSpline(
                    Matrix4x4.identity,
                    spline,
                    placementRadius,
                    childComponentCount,
                    rotationPerChild,
                    childBeginRange,
                    childEndRange)
                .Select(x => x * Matrix4x4.Scale(Vector3.one * childSizeReduction))
                .Select(x => x * Matrix4x4.TRS(
                    new Vector3(NextRand(rnd), 0f, NextRand(rnd)),
                    Quaternion.Euler(180 * NextRand(rnd), 180 * NextRand(rnd), 180 * NextRand(rnd)),
                    Vector3.one + new Vector3(NextRand(rnd), NextRand(rnd), NextRand(rnd)))
                );

            foreach (var childTransform in childTransforms)
            {
                output.AddWithTransform(childMesh.meshDraft, childTransform);
            }
            this.output = new MeshDraftWithExtras(output);
        }
        private float NextRand(System.Random rand)
        {
            return (float)((rand.NextDouble() * 2 - 1) * childRandomFactor);
        }
    }
}