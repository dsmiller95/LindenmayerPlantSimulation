using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class MeshDraft
    {
        public List<Vector3> vertices;
        public List<Vector2> uvs;
        public List<Vector3> normals;
        public List<Vector4> tangents;
        public List<int> triangles;

        public int vertexCount => vertices.Count;

        public MeshDraft(Mesh sourceMesh)
        {
            this.vertices = sourceMesh.vertices.ToList();
            this.uvs = sourceMesh.uv.ToList();
            this.normals = sourceMesh.normals.ToList();
            this.tangents = sourceMesh.tangents.ToList();
            this.triangles = sourceMesh.triangles.ToList();
        }

        public void Move(Vector3 translate)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i] + translate;
            }
        }

        public void Scale(Vector3 scale)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], scale);
                normals[i] = Vector3.Scale(normals[i], scale).normalized;
            }
        }
    }
}
