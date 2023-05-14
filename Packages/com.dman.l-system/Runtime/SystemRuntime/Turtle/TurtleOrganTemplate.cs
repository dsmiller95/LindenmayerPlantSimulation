using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.NativeCollections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganTemplate : ITurtleNativeDataWritable
    {
        public MeshDraft draft;
        public Material material;
        public Vector3 translation;
        public bool alsoMove;
        public Matrix4x4 meshTransform;

        public TurtleOrganTemplate(
            MeshDraft draft,
            Material material,
            Vector3 translation,
            bool shouldMove,
            Matrix4x4 meshTransform)
        {
            this.draft = draft;
            this.material = material;
            this.translation = translation;
            this.alsoMove = shouldMove;
            this.meshTransform = meshTransform;
        }

        public TurtleDataRequirements DataReqs => new TurtleDataRequirements
        {
            organTemplateSize = 1,
            vertextDataSize = draft.vertexCount,
            triangleDataSize = draft.triangles.Length
        };

        public void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            var vertexSlice = new JaggedIndexing
            {
                index = writer.indexInVertexes,
                length = (ushort)draft.vertexCount
            };
            for (int i = 0; i < vertexSlice.length; i++)
            {
                var vertexDatum = new NativeVertexDatum
                {
                    vertex = draft.vertices[i],
                    normal = draft.normals[i],
                    uv = draft.uvs[i],
                    tangent = draft.tangents[i]
                };
                nativeData.vertexData[i + writer.indexInVertexes] = vertexDatum;
            }
            writer.indexInVertexes += vertexSlice.length;

            var triangleCount = new JaggedIndexing
            {
                index = writer.indexInTriangles,
                length = (ushort)draft.triangles.Length
            };
            for (int i = 0; i < triangleCount.length; i++)
            {
                nativeData.triangleData[i + writer.indexInTriangles] = draft.triangles[i];
            }
            writer.indexInTriangles += triangleCount.length;

            var existingMaterialIndex = writer.GetMaterialIndex(material);
            var blittable = new Blittable
            {
                translation = translation,
                alsoMove = alsoMove,
                vertexes = vertexSlice,
                baseMeshTransform = meshTransform,
                trianges = triangleCount,
                materialIndex = (byte)existingMaterialIndex
            };
            nativeData.allOrganData[writer.indexInOrganTemplates] = blittable;
            writer.indexInOrganTemplates++;
        }

        public struct Blittable
        {
            /// <summary>
            /// the transformation to apply to the turtle after placing this organ
            /// </summary>
            public Vector3 translation;
            public bool alsoMove;
            public byte materialIndex;

            public Matrix4x4 baseMeshTransform;
            public JaggedIndexing vertexes;
            public JaggedIndexing trianges;
        }
    }
}
