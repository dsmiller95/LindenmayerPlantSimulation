using ProceduralToolkit;
using SplineMesh;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    public struct MeshDraftWithExtras
    {
        private MeshDraft meshDraft;

        public Bounds bounds { get; private set; }

        public MeshDraftWithExtras(MeshDraft draft, Bounds boundsOverwrite = default)
        {
            meshDraft = draft;
            bounds = boundsOverwrite;
            if (bounds == default)
                CalculateBounds();
        }

        public Mesh ToMesh()
        {
            var mesh = meshDraft.ToMesh(false, true);
            mesh.bounds = bounds;
            return mesh;
        }

        public MeshDraftWithExtras WrapSplineAll(CubicBezierCurve curve)
        {
            return new MeshDraftWithExtras(
                meshDraft.WrapSplineAll(curve, bounds.min.x, bounds.max.x));
        }

        public MeshDraftWithExtras Transform(Matrix4x4 transformation)
        {
            var transformEulerRotation = transformation.rotation.eulerAngles;
            if (transformation.rotation == Quaternion.identity || (
                Mathf.Abs(transformEulerRotation.x % 90f) < 1e-5 &&
                Mathf.Abs(transformEulerRotation.y % 90f) < 1e-5 &&
                Mathf.Abs(transformEulerRotation.z % 90f) < 1e-5))
            {
                return new MeshDraftWithExtras(
                    meshDraft.Transform(transformation),
                    new Bounds(
                        transformation.MultiplyPoint(bounds.center),
                        transformation.MultiplyVector(bounds.size)
                    ));
            }
            else
            {
                return new MeshDraftWithExtras(
                    meshDraft.Transform(transformation));
            }
        }

        private void CalculateBounds()
        {
            var minExtent = float.MaxValue * Vector3.one;
            var maxExtent = float.MinValue * Vector3.one;

            foreach (var vertex in meshDraft.vertices)
            {
                if (vertex.x < minExtent.x)
                    minExtent.x = vertex.x;
                if (vertex.x > maxExtent.x)
                    maxExtent.x = vertex.x;

                if (vertex.y < minExtent.y)
                    minExtent.y = vertex.y;
                if (vertex.y > maxExtent.y)
                    maxExtent.y = vertex.y;

                if (vertex.z < minExtent.z)
                    minExtent.z = vertex.z;
                if (vertex.z > maxExtent.z)
                    maxExtent.z = vertex.z;
            }
            bounds = new Bounds((maxExtent + minExtent) / 2, maxExtent - minExtent);
        }
    }
}
