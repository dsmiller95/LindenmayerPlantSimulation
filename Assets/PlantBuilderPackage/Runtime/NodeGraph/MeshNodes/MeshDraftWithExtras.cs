using ProceduralToolkit;
using SplineMesh;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    public struct MeshDraftWithExtras
    {
        public CompoundMeshDraft meshDraft { get; private set; }

        public Bounds bounds { get; private set; }

        public MeshDraftWithExtras(MeshDraft draft, Bounds boundsOverwrite = default)
        {

            meshDraft = new CompoundMeshDraft();
            meshDraft.Add(draft);
            bounds = boundsOverwrite;
            if (bounds == default)
                CalculateBounds();
        }
        public MeshDraftWithExtras(CompoundMeshDraft draft, Bounds boundsOverwrite = default)
        {
            meshDraft = draft;
            bounds = boundsOverwrite;
            if (bounds == default)
                CalculateBounds();
        }

        public Mesh ToMesh(bool submeshes = false)
        {
            if (meshDraft == null) return null;
            Mesh mesh;
            if (submeshes)
            {
                meshDraft.MergeDraftsWithTheSameName();
                meshDraft.SortDraftsByName();
                mesh = meshDraft.ToMeshWithSubMeshes(false, true);
            }
            else
            {
                mesh = meshDraft.ToMeshDraft().ToMesh(false, true);
            }
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
            meshDraft.Transform(transformation);
            if (transformation.rotation == Quaternion.identity || (
                Mathf.Abs(transformEulerRotation.x % 90f) < 1e-5 &&
                Mathf.Abs(transformEulerRotation.y % 90f) < 1e-5 &&
                Mathf.Abs(transformEulerRotation.z % 90f) < 1e-5))
            {

                return new MeshDraftWithExtras(
                    meshDraft,
                    new Bounds(
                        transformation.MultiplyPoint(bounds.center),
                        transformation.MultiplyVector(bounds.size).AbsoluteValue()
                    ));
            }
            else
            {
                return new MeshDraftWithExtras(
                    meshDraft);
            }
        }

        private void CalculateBounds()
        {
            var minExtent = float.MaxValue * Vector3.one;
            var maxExtent = float.MinValue * Vector3.one;
            foreach (var draft in meshDraft)
            {
                foreach (var vertex in draft.vertices)
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
            }
            bounds = new Bounds((maxExtent + minExtent) / 2, maxExtent - minExtent);
        }
    }
}
