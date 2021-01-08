using ProceduralToolkit.SplineMesh;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralToolkit
{
    public partial class MeshDraft
    {


        /// <summary>
        /// Moves draft vertices by <paramref name="vector"/>
        /// </summary>
        public MeshDraft WrapSplineOnce(CubicBezierCurve curve, float minX = 0)
        {
            var sampleCache = new Dictionary<float, CurveSample>();
            //var bentVertices = new List<MeshVertex>(vertexCount);
            // for each mesh vertex, we found its projection on the curve
            for (var vertIndex = 0; vertIndex < vertexCount; vertIndex++)
            {
                var vert = vertices[vertIndex];
                float distance = vert.x - minX;
                Debug.Log($"sampling at {distance}");
                if(distance < 0)
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
    }
}
