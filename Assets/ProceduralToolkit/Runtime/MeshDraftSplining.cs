using ProceduralToolkit.SplineMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralToolkit
{
    public partial class MeshDraft
    {


        /// <summary>
        /// Moves draft vertices by <paramref name="vector"/>
        /// </summary>
        public MeshDraft WrapSplineOnce(CubicBezierCurve curve, float minX = 0, float curveOffset = 0)
        {
            var sampleCache = new Dictionary<float, CurveSample>();
            //var bentVertices = new List<MeshVertex>(vertexCount);
            // for each mesh vertex, we found its projection on the curve
            for (var vertIndex = 0; vertIndex < vertexCount; vertIndex++)
            {
                var vert = vertices[vertIndex];
                float distance = vert.x - minX + curveOffset;
                Debug.Log($"sampling at {distance}");
                if (distance < 0)
                {
                    Debug.LogWarning($"Distance less than 0: {distance}. setting to 0");
                    distance = 0;
                }
                CurveSample sample;
                if (!sampleCache.TryGetValue(distance, out sample))
                {
                    if (distance > curve.Length) distance = curve.Length;
                    sample = curve.GetSampleAtDistance(distance);
                    sampleCache[distance] = sample;
                }

                var normal = normals[vertIndex];

                var bentVertexAndNormal = sample.GetBent(new MeshVertex(vert, normal, Vector2.one));

                vertices[vertIndex] = bentVertexAndNormal.position;
                normals[vertIndex] = bentVertexAndNormal.normal;
            }

            return this;
        }

        /// <summary>
        /// Moves draft vertices by <paramref name="vector"/>
        /// </summary>
        public MeshDraft WrapSplineAll(CubicBezierCurve curve, float minX = 0, float maxX = 1)
        {
            float intervalLength = curve.Length;
            int repetitionCount = Mathf.FloorToInt(intervalLength / (maxX - minX));

            this.DuplicateSelf(repetitionCount, new Vector3(maxX - minX, 0, 0));
            this.WrapSplineOnce(curve, minX, 0);
            return this;
        }

        public void DuplicateSelf(int times, Vector3 vectorOffset)
        {

            // building triangles and UVs for the repeated mesh
            var triangles = new List<int>(this.triangles.Count * times);
            var uv = new List<Vector2>(this.uv.Count * times);
            var uv2 = new List<Vector2>(this.uv2.Count * times);
            var uv3 = new List<Vector2>(this.uv3.Count * times);
            var uv4 = new List<Vector2>(this.uv4.Count * times);

            var colors = new List<Color>(this.colors.Count * times);
            var tangents = new List<Vector4>(this.tangents.Count * times);
            var normals = new List<Vector3>(this.normals.Count * times);

            var vertices = new List<Vector3>(this.vertices.Count * times);
            for (int i = 0; i < times; i++)
            {
                foreach (var index in this.triangles)
                {
                    triangles.Add(index + vertexCount * i);
                }
                uv.AddRange(this.uv);
                uv2.AddRange(this.uv2);
                uv3.AddRange(this.uv3);
                uv4.AddRange(this.uv4);

                colors.AddRange(this.colors);
                tangents.AddRange(this.tangents);
                normals.AddRange(this.normals);

                vertices.AddRange(this.vertices.Select(x => x + vectorOffset * i));
            }

            this.triangles = triangles;
            this.uv = uv;
            this.uv2 = uv2;
            this.uv3 = uv3;
            this.uv4 = uv4;

            this.colors = colors;
            this.tangents = tangents;
            this.normals = normals;

            this.vertices = vertices;
        }
    }
}
