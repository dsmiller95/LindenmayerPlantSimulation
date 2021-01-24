using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantBuilder
{
    [CreateAssetMenu(fileName = "SplinedComponent", menuName = "Builders/SplinedComponent")]
    public class SplineFitBuilder : ComponentBuilder
    {
        public int childComponentCount = 10;
        public float rotationPerChild = 360 * 3 / 5;

        public SplineNode splineNode1;
        public SplineNode splineNode2;

        [Range(0, 1f)]
        public float childBeginRange = .3f;
        [Range(0, 1f)]
        public float childEndRange = .7f;
        public float childRandomFactor = .05f;
        public float childSizeReduction = 0.2f;


        [Tooltip("Mesh to tile across the spline. The x-axis is what is mapped to the spline.")]
        public Mesh splinedMesh;
        public float meshLengthStretch = 1;
        public float meshPlaneStretch = 1;

        public override MeshDraft CreateComponentMesh(
            Matrix4x4 meshTransform,
            int componentLevel,
            Stack<NextComponentSpawnCommand> extraComponents,
            System.Random rand)
        {
            var bezier = new CubicBezierCurve(splineNode1, splineNode2);

            var childTransforms = SplineTransformPlacement.GenerateTransformationsAlongSpline(
                    meshTransform,
                    bezier,
                    meshPlaneStretch,
                    childComponentCount,
                    rotationPerChild,
                    childBeginRange,
                    childEndRange)
                .Select(x => x * Matrix4x4.Scale(Vector3.one * childSizeReduction))
                .Select(x => x * Matrix4x4.TRS(
                    new Vector3(NextRand(rand), 0f, NextRand(rand)),
                    Quaternion.Euler(180 * NextRand(rand), 180 * NextRand(rand), 180 * NextRand(rand)),
                    Vector3.one + new Vector3(NextRand(rand), NextRand(rand), NextRand(rand)))
                );

            foreach (var childTransform in childTransforms)
            {
                extraComponents.Push(new NextComponentSpawnCommand
                {
                    componentIndex = componentLevel + 1,
                    componentTransformation = childTransform
                });
            }

            var resultDraft = new MeshDraft(splinedMesh);// MeshDraft.Cylinder(meshPlaneStretch, 9, meshLengthStretch, false);
            var originalBounds = splinedMesh.bounds;
            var boundSize = originalBounds.size;
            // push the mesh out so that all x > 0
            //resultDraft.Move(new Vector3(-originalBounds.center.x, 0, 0));
            var scaleVector = new Vector3(meshLengthStretch, meshPlaneStretch, meshPlaneStretch);
            resultDraft.Scale(scaleVector);
            boundSize.Scale(scaleVector);
            //resultDraft.DuplicateSelf(3, new Vector3(height, 0, 0));
            resultDraft.WrapSplineAll(bezier, originalBounds.center.x - boundSize.x / 2, originalBounds.center.x + boundSize.x / 2);
            //resultDraft.Rotate(Quaternion.Euler(0, 0, 90));
            resultDraft.Transform(meshTransform);
            return resultDraft;
        }


        private float NextRand(System.Random rand)
        {
            return (float)((rand.NextDouble() * 2 - 1) * childRandomFactor);
        }
    }
}