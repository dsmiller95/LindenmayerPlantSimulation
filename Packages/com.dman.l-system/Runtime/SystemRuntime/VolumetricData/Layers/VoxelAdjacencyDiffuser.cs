using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    /// <summary>
    /// this diffuser diffuses only to adjacent voxels. not as high quality as the kernel diffuser, but can handle
    ///     boundary conditions better.
    /// </summary>
    public class VoxelAdjacencyDiffuser
    {
        /// <summary>
        /// takes in a by-voxel data array. returns another array of the same format with the diffused results.
        ///     may modify the values in the input array. may return the input array. will handle disposing the input
        ///     array if not returned.
        /// </summary>
        public static NativeArray<float> ComputeDiffusion(
            VolumetricWorldVoxelLayout voxelLayout,
            NativeArray<float> inputArrayWithData,
            float deltaTime,
            float diffusionConstant,
            ref JobHandle dependecy)
        {
            var tmpSwapSpace = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.TempJob);

            var combinedDiffusionFactor = deltaTime * diffusionConstant;
            if(combinedDiffusionFactor >= 1f / 6)
            {
                throw new System.ArgumentException("diffusion factor cannot exceed the connectivity of each node");
            }

            var adjacencyVectors = new NativeArray<Vector3Int>(new[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0,-1, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0,-1),
            }, Allocator.TempJob);

            var diffuseJob = new VoxelAdjacencyResourceConservingBoundaryComputeJob
            {
                sourceDiffusionValues = inputArrayWithData,
                targetDiffusionValues = tmpSwapSpace,

                adjacencyVectors = adjacencyVectors,

                voxelLayout = voxelLayout,

                diffusionConstant = combinedDiffusionFactor
            };
            dependecy = diffuseJob.Schedule(inputArrayWithData.Length, 1000, dependecy);

            inputArrayWithData.Dispose(dependecy);
            adjacencyVectors.Dispose(dependecy);

            return tmpSwapSpace;
        }

        [BurstCompile]
        struct VoxelAdjacencyResourceConservingBoundaryComputeJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float> sourceDiffusionValues;
            public NativeArray<float> targetDiffusionValues;

            [ReadOnly]
            public NativeArray<Vector3Int> adjacencyVectors;

            public VolumetricWorldVoxelLayout voxelLayout;

            public float diffusionConstant;

            public void Execute(int index)
            {
                var voxelIndex = new VoxelIndex
                {
                    Value = index
                };
                var rootCoordiante = voxelLayout.GetCoordinatesFromVoxelIndex(voxelIndex);
                var originalSelfValue = sourceDiffusionValues[voxelIndex.Value];

                float newValue = originalSelfValue;

                for (int adjacencyIndex = 0; adjacencyIndex < adjacencyVectors.Length; adjacencyIndex++)
                {
                    var offset = adjacencyVectors[adjacencyIndex];
                    var sampleCoordinate = offset + rootCoordiante;
                    var sampleIndex = voxelLayout.GetVoxelIndexFromCoordinates(sampleCoordinate);

                    float sampleValue;
                    if (!sampleIndex.IsValid)
                    {
                        // if the index is outside of bounds, do no diffusion
                        //  this should have an effect of conserving all resources inside the bounds, none
                        //  should be lost or gained from the boundary conditions
                        continue;
                    }
                    else
                    {
                        sampleValue = sourceDiffusionValues[sampleIndex.Value];
                    }

                    var diffuseAmount = (sampleValue - originalSelfValue) * diffusionConstant;
                    newValue += diffuseAmount;
                }

                targetDiffusionValues[voxelIndex.Value] = newValue;
            }
        }
    }
}
