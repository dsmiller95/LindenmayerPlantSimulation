//using ProceduralToolkit;
//using SplineMesh;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//namespace PlantBuilder
//{
//    [CreateAssetMenu(fileName = "ComponentMeshSource", menuName = "Builders/ComponentMeshSource")]
//    public class ComponentMeshSource : ComponentBuilder
//    {
//        public Mesh sourceMesh;

//        public override MeshDraft CreateComponentMesh(
//            Matrix4x4 meshTransform,
//            int componentLevel,
//            Stack<NextComponentSpawnCommand> extraComponents,
//            System.Random rand)
//        {
//            var bezier = new CubicBezierCurve(splineNode1, splineNode2);

//            var childTransforms = SplineTransformPlacement.GenerateTransformationsAlongSpline(
//                    meshTransform,
//                    bezier,
//                    meshPlaneStretch,
//                    childComponentCount,
//                    rotationPerChild,
//                    childBeginRange,
//                    childEndRange)
//                .Select(x => x * Matrix4x4.Scale(Vector3.one * childSizeReduction))
//                .Select(x => x * Matrix4x4.TRS(
//                    new Vector3(NextRand(rand), 0f, NextRand(rand)),
//                    Quaternion.Euler(180 * NextRand(rand), 180 * NextRand(rand), 180 * NextRand(rand)),
//                    Vector3.one + new Vector3(NextRand(rand), NextRand(rand), NextRand(rand)))
//                );

//            foreach (var childTransform in childTransforms)
//            {
//                extraComponents.Push(new NextComponentSpawnCommand
//                {
//                    componentIndex = componentLevel + 1,
//                    componentTransformation = childTransform
//                });
//            }

//            var resultDraft = new MeshDraft(splinedMesh);// MeshDraft.Cylinder(meshPlaneStretch, 9, meshLengthStretch, false);
//            resultDraft.Move(new Vector3(0, .5f, 0));
//            resultDraft.Rotate(Quaternion.Euler(0, 0, -90));
//            resultDraft.Scale(new Vector3(meshLengthStretch, meshPlaneStretch, meshPlaneStretch));
//            //resultDraft.DuplicateSelf(3, new Vector3(height, 0, 0));
//            resultDraft.WrapSplineAll(bezier, 0, meshLengthStretch);
//            //resultDraft.Rotate(Quaternion.Euler(0, 0, 90));
//            resultDraft.Transform(meshTransform);
//            return resultDraft;
//        }


//        private float NextRand(System.Random rand)
//        {
//            return (float)((rand.NextDouble() * 2 - 1) * childRandomFactor);
//        }
//    }
//}