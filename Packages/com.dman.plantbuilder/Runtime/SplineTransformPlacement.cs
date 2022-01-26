using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder
{
    public class SplineTransformPlacement
    {
        public SplineNode splineNode1;
        public SplineNode splineNode2;

        public static IEnumerable<Matrix4x4> GenerateTransformationsAlongSpline(
            Matrix4x4 rootTransform,
            CubicBezierCurve spline,
            float radius,
            float childComponentCount,
            float rotationPerChildDegrees,
            float childBeginRange,
            float childEndRange)
        {
            var baseChildTransform = rootTransform;
            for (int childIndex = 0; childIndex < childComponentCount; childIndex++)
            {
                var translateOnCurve = ((float)childIndex / childComponentCount) * (childEndRange - childBeginRange) + childBeginRange;

                var sample = spline.GetSampleAtDistance(translateOnCurve * spline.Length);


                var childTransform = baseChildTransform
                    * Matrix4x4.Translate(sample.location)
                    * Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.up, sample.tangent))
                    * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0))
                    * Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotationPerChildDegrees * childIndex))
                    * Matrix4x4.Translate(new Vector3(0, radius * sample.scale.x, 0));// TODO: asymmetric scale how?

                yield return childTransform;
            }
        }
    }
}