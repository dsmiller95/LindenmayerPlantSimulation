using Dman.LSystem.Extern;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.Utilities.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct MeshVertexLayout
    {
        public float3 pos;
        public float3 normal;
        public Color32 color;
        public float2 uv;
        public byte4 extraData;
    }

    internal struct TurtleMeshData : INativeDisposable
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<MeshVertexLayout> vertexData;
        [NativeDisableParallelForRestriction]
        public NativeArray<uint> indices;
        public NativeArray<Bounds> meshBoundsBySubmesh;

        public static VertexAttributeDescriptor[] GetVertexLayout()
        {
            return new[]{
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.UNorm8, 4)
                };
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                vertexData.Dispose(inputDeps),
                indices.Dispose(inputDeps),
                meshBoundsBySubmesh.Dispose(inputDeps));
        }

        public void Dispose()
        {
            vertexData.Dispose();
            indices.Dispose();
            meshBoundsBySubmesh.Dispose();
        }
    }

    internal struct OrganMeshMemorySpaceAllocation
    {
        public JaggedIndexing vertexMemorySpace;
        public JaggedIndexing trianglesMemorySpace;
    }
}
