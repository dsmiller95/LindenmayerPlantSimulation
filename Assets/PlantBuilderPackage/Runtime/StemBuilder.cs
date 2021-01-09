using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder
{
    [CreateAssetMenu(fileName = "StemComponent", menuName = "Builders/StemComponent")]
    public class StemBuilder : ComponentBuilder
    {
        public float height = 1;
        public float radius = 1;

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


        public override MeshDraft CreateComponentMesh(
            Matrix4x4 meshTransform,
            int componentLevel,
            Stack<NextComponentSpawnCommand> extraComponents,
            System.Random rand)
        {
            var bezier = new CubicBezierCurve(splineNode1, splineNode2);

            var baseChildTransform = meshTransform;
            for (int childIndex = 0; childIndex < childComponentCount; childIndex++)
            {
                var translateOnCurve = ((float)childIndex / childComponentCount) * (childEndRange - childBeginRange) + childBeginRange;

                var sample = bezier.GetSampleAtDistance(translateOnCurve * bezier.Length);


                var childTransform = baseChildTransform
                    * Matrix4x4.Translate(sample.location)
                    * Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.up, sample.tangent))
                    * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0))
                    * Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotationPerChild * childIndex))
                    //* Matrix4x4.TRS(
                    //sample.location,
                    ////Quaternion.identity,
                    //Quaternion.Euler(0, 0, rotationPerChild * childIndex),
                    //Vector3.one);
                    ;
                childTransform *= Matrix4x4.Translate(new Vector3(0, radius * sample.scale.x, 0));// TODO: asymmetric scale how?
                childTransform *= Matrix4x4.Scale(Vector3.one * childSizeReduction);
                //childTransform *= Matrix4x4.Translate(new Vector3(0, -radius, 0));
                //childTransform *= Matrix4x4.TRS(
                //    new Vector3(NextRand(rand), 0f, NextRand(rand)),
                //    Quaternion.Euler(180 * NextRand(rand), 180 * NextRand(rand), 180 * NextRand(rand)),
                //    Vector3.one + new Vector3(NextRand(rand), NextRand(rand), NextRand(rand)));
                extraComponents.Push(new NextComponentSpawnCommand
                {
                    componentIndex = componentLevel + 1,
                    componentTransformation = childTransform
                });
            }

            var resultDraft = MeshDraft.Cylinder(radius, 9, height, false);
            resultDraft.Move(new Vector3(0, height / 2, 0));
            resultDraft.Rotate(Quaternion.Euler(0, 0, -90));
            //resultDraft.DuplicateSelf(3, new Vector3(height, 0, 0));
            resultDraft.WrapSplineAll(bezier, 0, height);
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