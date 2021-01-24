using ProceduralToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantBuilder
{
    public static class MeshDraftExtensions
    {

        public static void AddWithTransform(this CompoundMeshDraft self, CompoundMeshDraft compoundDraft, Matrix4x4 geometryTransform)
        {
            foreach (var draft in compoundDraft)
            {
                var newDraft = new MeshDraft();
                newDraft.name = draft.name;
                newDraft.AddWithTransform(draft, geometryTransform);
                self.Add(newDraft);
            }
        }

        public static void AddWithTransform(this MeshDraft self, MeshDraft draft, Matrix4x4 geometryTransform)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));

            for (var i = 0; i < draft.triangles.Count; i++)
            {
                self.triangles.Add(draft.triangles[i] + self.vertices.Count);
            }
            self.vertices.AddRange(draft.vertices.Select(x => geometryTransform.MultiplyPoint(x)));
            self.normals.AddRange(draft.normals.Select(x => geometryTransform.MultiplyVector(x)));
            self.tangents.AddRange(draft.tangents);
            self.uv.AddRange(draft.uv);
            self.uv2.AddRange(draft.uv2);
            self.uv3.AddRange(draft.uv3);
            self.uv4.AddRange(draft.uv4);
            self.colors.AddRange(draft.colors);
        }

        public static void Transform(this CompoundMeshDraft self, Matrix4x4 transformation)
        {
            foreach (var draft in self)
            {
                draft.Transform(transformation);
            }
        }

        public static void Transform(this MeshDraft self, Matrix4x4 transformation)
        {
            for (int i = 0; i < self.vertices.Count; i++)
            {
                self.vertices[i] = transformation.MultiplyPoint(self.vertices[i]);
                self.normals[i] = transformation.MultiplyVector(self.normals[i]).normalized;
            }
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

            //if (self.uv.Count != self.vertexCount || self.tangents.Count != self.vertexCount)
            //{
            //    Debug.LogError("problem with uv and tangent counts on import");
            //}
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

            //Debug.Log($"Creating big array this big {self.vertexCount - uv.Count}");
            var extraUVs = new Vector2[vertices.Count - uv.Count];
            uv.AddRange(extraUVs);
            var extraTangents = new Vector4[vertices.Count - tangents.Count];
            tangents.AddRange(extraTangents);

            self.triangles = triangles;
            self.uv = uv;
            self.uv2 = uv2;
            self.uv3 = uv3;
            self.uv4 = uv4;

            self.colors = colors;
            self.tangents = tangents;
            self.normals = normals;

            self.vertices = vertices;

            //if(self.uv.Count != self.vertexCount || self.tangents.Count != self.vertexCount)
            //{
            //    Debug.LogError("problem with uv and tangent counts");
            //}
        }
    }
}
