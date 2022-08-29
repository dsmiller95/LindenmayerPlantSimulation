using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.Utilities.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [BurstCompile]
    internal struct TurtleMeshBoundsJob : IJob
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<TurtleMeshAllocationCounter> submeshSizes;

        [NativeDisableParallelForRestriction]
        public TurtleMeshData targetMesh;
        public void Execute()
        {
            var vertexTargetData = targetMesh.vertexData;
            for (int index = 0; index < submeshSizes.Length; index++)
            {
                var submeshData = submeshSizes[index];
                var bounds = targetMesh.meshBoundsBySubmesh[index];
                for (int vertexIndex = 0; vertexIndex < submeshData.totalVertexes; vertexIndex++)
                {
                    var vertex = vertexTargetData[submeshData.indexInVertexes + vertexIndex];
                    bounds.Encapsulate(vertex.pos);
                }
                targetMesh.meshBoundsBySubmesh[index] = bounds;
            }
        }
    }
}
