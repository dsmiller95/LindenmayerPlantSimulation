using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class MeshDraft
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public Vector3[] normals;
        public Vector4[] tangents;
        public int[] triangles;

        public int vertexCount => vertices.Length;

        public MeshDraft(Mesh sourceMesh)
        {
            this.vertices = sourceMesh.vertices.ToArray();
            this.uvs = sourceMesh.uv.ToArray();
            this.normals = sourceMesh.normals.ToArray();
            this.tangents = sourceMesh.tangents.ToArray();
            this.triangles = sourceMesh.triangles.ToArray();
        }

        public void Move(Vector3 translate)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i] + translate;
            }
        }

        public void Scale(Vector3 scale)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], scale);
                normals[i] = Vector3.Scale(normals[i], scale).normalized;
            }
        }

        public void Transform(Matrix4x4 transform)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = transform.MultiplyPoint(vertices[i]);
                normals[i] = transform.MultiplyVector(normals[i]);
            }
        }
    }
}
