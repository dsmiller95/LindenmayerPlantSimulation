using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.MeshDraftExtensions
{
    public static class MeshDraftSpliningExtensions
    {
        /// <summary>
        /// Moves draft vertices by <paramref name="vector"/>
        /// </summary>
        public static void WrapSplineOnce(this MeshDraft self, CubicBezierCurve curve, float minX = 0, float curveOffset = 0)
        {
            var sampleCache = new Dictionary<float, CurveSample>();
            //var bentVertices = new List<MeshVertex>(vertexCount);
            // for each mesh vertex, we found its projection on the curve
            for (var vertIndex = 0; vertIndex < self.vertexCount; vertIndex++)
            {
                var vert = self.vertices[vertIndex];
                float distance = vert.x - minX + curveOffset;
                if (distance < 0)
                {
                    if (distance < -.01)
                    {
                        Debug.LogWarning($"Distance less than 0: {distance}. setting to 0");
                    }
                    distance = 0;
                }
                CurveSample sample;
                if (!sampleCache.TryGetValue(distance, out sample))
                {
                    if (distance > curve.Length) distance = curve.Length;
                    sample = curve.GetSampleAtDistance(distance);
                    sampleCache[distance] = sample;
                }

                var normal = self.normals[vertIndex];

                var bentVertexAndNormal = sample.GetBent(new MeshVertex(vert, normal, Vector2.one));

                self.vertices[vertIndex] = bentVertexAndNormal.position;
                self.normals[vertIndex] = bentVertexAndNormal.normal;
            }
        }

        /// <summary>
        /// Moves draft vertices by <paramref name="vector"/>
        /// </summary>
        public static void WrapSplineAll(this MeshDraft self, CubicBezierCurve curve, float minX = 0, float maxX = 1)
        {
            float intervalLength = curve.Length;
            int repetitionCount = Mathf.FloorToInt(intervalLength / (maxX - minX));

            self.DuplicateSelf(repetitionCount, new Vector3(maxX - minX, 0, 0));
            self.WrapSplineOnce(curve, minX, 0);
        }

        public static CompoundMeshDraft WrapSplineAll(this CompoundMeshDraft self, CubicBezierCurve curve, float minX = 0, float maxX = 1)
        {
            var newDraft = new CompoundMeshDraft();

            foreach (var meshDraft in self)
            {
                var newSingleDraft = new MeshDraft();
                newSingleDraft.Add(meshDraft);
                newSingleDraft.name = meshDraft.name;
                newSingleDraft.WrapSplineAll(curve, minX, maxX);
                newDraft.Add(newSingleDraft);
            }
            return newDraft;
        }
    }
}
