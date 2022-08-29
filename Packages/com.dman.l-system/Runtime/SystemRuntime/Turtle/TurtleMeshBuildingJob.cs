using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [BurstCompile]
    internal struct TurtleMeshBuildingJob : IJobParallelFor
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<NativeVertexDatum> templateVertexData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> templateTriangleData;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<TurtleOrganTemplate.Blittable> templateOrganData;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<TurtleMeshAllocationCounter> submeshSizes;

        [ReadOnly]
        public NativeArray<TurtleOrganInstance> organInstances;
        [ReadOnly]
        public NativeArray<OrganMeshMemorySpaceAllocation> organMeshAllocations;

        public TurtleMeshData targetMesh;

        public void Execute(int index)
        {
            var vertexTargetData = targetMesh.vertexData;
            var triangleIndexes = targetMesh.indices;

            var organInstance = organInstances[index];
            var organTemplate = templateOrganData[organInstance.organIndexInAllOrgans];
            var submeshData = submeshSizes[organTemplate.materialIndex];
            var matrixTransform = (Matrix4x4)organInstance.organTransform;

            var organMeshSpace = organMeshAllocations[index];
            var organVertexOffset = submeshData.indexInVertexes + organMeshSpace.vertexMemorySpace.index;
            var organTriangleOffset = submeshData.indexInTriangles + organMeshSpace.trianglesMemorySpace.index;

            for (int i = 0; i < organTemplate.vertexes.length; i++)
            {
                var sourceVertexData = templateVertexData[organTemplate.vertexes.index + i];
                var newVertexData = new MeshVertexLayout
                {
                    pos = matrixTransform.MultiplyPoint(sourceVertexData.vertex),
                    normal = matrixTransform.MultiplyVector(sourceVertexData.normal),
                    uv = sourceVertexData.uv,
                    color = ColorFromIdentity(organInstance.organIdentity, (uint)index),
                    extraData = organInstance.extraData
                };
                vertexTargetData[i + organVertexOffset] = newVertexData;
            }

            for (int i = 0; i < organTemplate.trianges.length; i++)
            {
                triangleIndexes[i + organTriangleOffset] = (uint)
                    (templateTriangleData[i + organTemplate.trianges.index]
                    + organVertexOffset); // offset the triangle indexes by the current index in vertex mem space
            }
        }

        private Color32 ColorFromIdentity(UIntFloatColor32 identity, uint index)
        {
            //identity.UIntValue = BitMixer.Mix(identity.UIntValue);

            return identity.color;
        }
    }
}
