using ProceduralToolkit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantBuilder
{
    public static class MeshDraftExtensions
    {

        public static MeshDraft Transform(this MeshDraft self, Matrix4x4 transformation)
        {
            for (int i = 0; i < self.vertices.Count; i++)
            {
                self.vertices[i] = transformation.MultiplyPoint(self.vertices[i]);
                self.normals[i] = transformation.MultiplyVector(self.normals[i]).normalized;
            }
            return self;
        }

        public static void DuplicateSelf(this MeshDraft self, int times, Vector3 vectorOffset)
        {

            // building triangles and UVs for the repeated mesh
            var triangles = new List<int>(self.triangles.Count * times);
            var uv = new List<Vector2>(self.uv.Count * times);
            var uv2 = new List<Vector2>(self.uv2.Count * times);
            var uv3 = new List<Vector2>(self.uv3.Count * times);
            var uv4 = new List<Vector2>(self.uv4.Count * times);

            var colors = new List<Color>(self.colors.Count * times);
            var tangents = new List<Vector4>(self.tangents.Count * times);
            var normals = new List<Vector3>(self.normals.Count * times);

            var vertices = new List<Vector3>(self.vertices.Count * times);
            for (int i = 0; i < times; i++)
            {
                foreach (var index in self.triangles)
                {
                    triangles.Add(index + self.vertexCount * i);
                }
                uv.AddRange(self.uv);
                uv2.AddRange(self.uv2);
                uv3.AddRange(self.uv3);
                uv4.AddRange(self.uv4);

                colors.AddRange(self.colors);
                tangents.AddRange(self.tangents);
                normals.AddRange(self.normals);

                vertices.AddRange(self.vertices.Select(x => x + vectorOffset * i));
            }

            self.triangles = triangles;
            self.uv = uv;
            self.uv2 = uv2;
            self.uv3 = uv3;
            self.uv4 = uv4;

            self.colors = colors;
            self.tangents = tangents;
            self.normals = normals;

            self.vertices = vertices;
        }
    }
}
