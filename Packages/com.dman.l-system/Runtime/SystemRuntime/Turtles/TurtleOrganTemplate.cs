using Dman.LSystem.SystemRuntime.NativeCollections;
using ProceduralToolkit;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganTemplate : ITurtleNativeDataWritable
    {
        public MeshDraft draft;
        public Material material;
        public Vector3 translation;
        public bool alsoMove;

        public TurtleOrganTemplate(
            MeshDraft draft,
            Material material,
            Vector3 translation,
            bool shouldMove)
        {
            this.draft = draft;
            this.material = material;
            this.translation = translation;
            this.alsoMove = shouldMove;
        }

        public TurtleDataRequirements DataReqs => new TurtleDataRequirements
        {
            organTemplateSize = 1,
            vertextDataSize = draft.vertexCount,
            triangleDataSize = draft.triangles.Count
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
                    uv = draft.uv[i],
                    tangent = draft.tangents[i]
                };
                nativeData.vertexData[i + writer.indexInVertexes] = vertexDatum;
            }
            writer.indexInVertexes += vertexSlice.length;

            var triangleCount = new JaggedIndexing
            {
                index = writer.indexInTriangles,
                length = (ushort)draft.triangles.Count
            };
            for (int i = 0; i < triangleCount.length; i++)
            {
                nativeData.triangleData[i + writer.indexInTriangles] = draft.triangles[i];
            }
            writer.indexInTriangles += triangleCount.length;

            var existingMaterialIndex = writer.materialsInOrder.IndexOf(material);
            if (existingMaterialIndex == -1)
            {
                existingMaterialIndex = writer.materialsInOrder.Count;
                writer.materialsInOrder.Add(material);
            }
            var blittable = new Blittable
            {
                translation = translation,
                alsoMove = alsoMove,
                vertexes = vertexSlice,
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

            public JaggedIndexing vertexes;
            public JaggedIndexing trianges;
        }
    }
}
