using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    /// <summary>
    /// this diffuser diffuses only to adjacent voxels. not as high quality as the kernel diffuser, but can handle
    ///     boundary conditions better.
    /// Also allows for setting diffusion adjustments per-voxel, which allows for diffusion to be limited
    ///     to certain volumes, defined by the voxel array
    /// </summary>
    public class VoxelAdjacencyByVoxelConstantDiffuser
    {
        /// <summary>
        /// takes in a by-voxel data array. returns another array of the same format with the diffused results.
        ///     may modify the values in the input array. may return the input array. will handle disposing the input
        ///     array if not returned.
        /// </summary>
        public static void ComputeDiffusion(
            VolumetricWorldVoxelLayout voxelLayout,
            DoubleBuffered<float> layerData,
            NativeArray<float> diffusionConstantMultipliers,
            float minimumDiffusionConstantMultiplier,
            float deltaTime,
            float diffusionConstant,
            ref JobHandle dependecy)
        {
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
                sourceDiffusionValues = layerData.CurrentData,
                targetDiffusionValues = layerData.NextData,

                diffusionConstantAdjusters = diffusionConstantMultipliers,
                minimumDiffusionConstantMultiplier = minimumDiffusionConstantMultiplier,
                maximumDiffsuionConstant = 1/7f,

                adjacencyVectors = adjacencyVectors,

                voxelLayout = voxelLayout,

                diffusionConstant = combinedDiffusionFactor
            };
            dependecy = diffuseJob.Schedule(layerData.CurrentData.Length, 1000, dependecy);
            layerData.Swap();

            adjacencyVectors.Dispose(dependecy);
        }

        [BurstCompile]
        struct VoxelAdjacencyResourceConservingBoundaryComputeJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float> sourceDiffusionValues;
            public NativeArray<float> targetDiffusionValues;

            [ReadOnly]
            public NativeArray<float> diffusionConstantAdjusters;
            public float minimumDiffusionConstantMultiplier;
            public float maximumDiffsuionConstant;

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
                var selfDiffusionConstantAdjustment = math.max(diffusionConstantAdjusters[voxelIndex.Value], minimumDiffusionConstantMultiplier);

                float newValue = originalSelfValue;

                for (int adjacencyIndex = 0; adjacencyIndex < adjacencyVectors.Length; adjacencyIndex++)
                {
                    var offset = adjacencyVectors[adjacencyIndex];
                    var sampleCoordinate = offset + rootCoordiante;
                    var sampleIndex = voxelLayout.GetVoxelIndexFromCoordinates(sampleCoordinate);

                    if (!sampleIndex.IsValid)
                    {
                        // if the index is outside of bounds, do no diffusion
                        //  this should have an effect of conserving all resources inside the bounds, none
                        //  should be lost or gained from the boundary conditions
                        continue;
                    }

                    var sampleValue = sourceDiffusionValues[sampleIndex.Value];
                    var otherDiffusionConstantAdjustment = math.max(diffusionConstantAdjusters[sampleIndex.Value], minimumDiffusionConstantMultiplier);
                    var diffusionAdjustment = (selfDiffusionConstantAdjustment + otherDiffusionConstantAdjustment) / 2f;

                    var diffuseAmount = (sampleValue - originalSelfValue) * math.min(diffusionConstant * diffusionAdjustment, maximumDiffsuionConstant);
                    newValue += diffuseAmount;
                }

                targetDiffusionValues[voxelIndex.Value] = newValue;
            }
        }
    }
}
