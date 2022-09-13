using Dman.LSystem.SystemRuntime.Turtle;
using Dman.Utilities.Math;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects.StemTrunk
{
    [BurstCompile]
    internal struct TurtleStemBuildingJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<TurtleMeshAllocationCounter> submeshSizes;

        [ReadOnly]
        public NativeArray<TurtleStemInstance> stemInstances;
        [ReadOnly]
        public NativeArray<TurtleStemGenerationData> generationData;
        [ReadOnly]
        public NativeArray<OrganMeshMemorySpaceAllocation> organMeshAllocations;

        public int meshMemoryOffset;

        public TurtleMeshData targetMesh;

        public void Execute(int stemIndex)
        {
            var vertexTargetData = targetMesh.vertexData;
            var triangleIndexes = targetMesh.indices;

            var stemInstance = stemInstances[stemIndex];
            var generationParameters = generationData[stemIndex];
            var pointTransform = stemInstance.orientation;
            var meshMemorySpace = organMeshAllocations[stemIndex + meshMemoryOffset];
            var submeshData = submeshSizes[stemInstance.materialIndex];

            var vertexOffset = submeshData.indexInVertexes + meshMemorySpace.vertexMemorySpace.index;

            var normalizedAccumulateRotationalOffset = generationParameters.normalizedVertexAngleOffset;

            for (int theta = 0; theta <= stemInstance.radialResolution; theta++)
            {
                var normalized = theta / (float)stemInstance.radialResolution;
                // rotate the vertexes themselves based on rotation of the parent, if any
                var radians = (normalized + normalizedAccumulateRotationalOffset) * math.PI * 2f;
                var normal = new float3(0, math.sin(radians), math.cos(radians));
                var point = normal;
                point.x = 0.5f;
                vertexTargetData[theta + vertexOffset] = new MeshVertexLayout
                {
                    pos = pointTransform.MultiplyPoint3x4(point),
                    normal = pointTransform.MultiplyVector(normal),
                    uv = new float2(normalized, generationParameters.uvDepth),
                    color = ColorFromIdentity(stemInstance.organIdentity, (uint)stemIndex),
                    extraData = stemInstance.extraData
                };
            }
            if (Hint.Unlikely(stemInstance.parentIndex < 0))
            {
                return;
            }
            var parentStem = stemInstances[stemInstance.parentIndex];
            var triangleOffset = submeshData.indexInTriangles + meshMemorySpace.trianglesMemorySpace.index;
            if (Hint.Unlikely(parentStem.radialResolution != stemInstance.radialResolution || parentStem.materialIndex != stemInstance.materialIndex))
            {
                for (int i = 0; i < meshMemorySpace.trianglesMemorySpace.length; i++)
                {
                    // clear out triangle indexes to 0 here
                    triangleIndexes[i + triangleOffset] = 0;
                }
                return;
            }
            var parentStemMeshMemory = organMeshAllocations[stemInstance.parentIndex + meshMemoryOffset];
            // create the rectangle strip. only supported when equal radial vertex count and same submesh
            var parentVertexOffset = submeshData.indexInVertexes + parentStemMeshMemory.vertexMemorySpace.index;
            for (int rectIndex = 0; rectIndex < stemInstance.radialResolution; rectIndex++)
            {
                // intentionally avoid using modulo here. this is since the circle is not complete, there is a duplicate vertex
                //  at index 0 and index n
                var nextIndex = rectIndex + 1;
                var p1 = (uint)(rectIndex + parentVertexOffset);
                var p2 = (uint)(nextIndex + parentVertexOffset);

                var c1 = (uint)(rectIndex + vertexOffset);
                var c2 = (uint)(nextIndex + vertexOffset);

                var triangleBase = rectIndex * 6 + triangleOffset;
                triangleIndexes[triangleBase + 0] = p1;
                triangleIndexes[triangleBase + 1] = c1;
                triangleIndexes[triangleBase + 2] = c2;

                triangleIndexes[triangleBase + 3] = p1;
                triangleIndexes[triangleBase + 4] = c2;
                triangleIndexes[triangleBase + 5] = p2;
            }
        }

        private Color32 ColorFromIdentity(UIntFloatColor32 identity, uint index)
        {
            //identity.UIntValue = BitMixer.Mix(identity.UIntValue);

            return identity.color;
        }
    }
}
