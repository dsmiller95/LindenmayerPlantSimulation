using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace
{
    /// <summary>
    /// takes old and new copies of data and updates the base data
    ///     by subtracting the old, and adding the new
    /// </summary>
    [BurstCompile]
    public struct VoxelMarkerConsolidation : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> newMarkerLevels;
        [ReadOnly]
        public NativeArray<float> oldMarkerLevels;

        [NativeDisableParallelForRestriction]
        public VoxelWorldVolumetricLayerData allBaseMarkers;

        public int markerLayerIndex;

        public void Execute(int index)
        {
            var voxelIndex = new VoxelIndex
            {
                Value = index
            };
            allBaseMarkers[voxelIndex, markerLayerIndex] = Mathf.Max(allBaseMarkers[voxelIndex, markerLayerIndex] + newMarkerLevels[index] - oldMarkerLevels[index], 0);
        }
    }
    public struct NativeArrayAdditionNegativeProtection : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> layerToAdd;
        public float layerMultiplier;

        [NativeDisableParallelForRestriction]
        public VoxelWorldVolumetricLayerData allBaseMarkers;
        public int totalLayersInBase;
        public int markerLayerIndex;

        public void Execute(int index)
        {
            var voxelIndex = new VoxelIndex
            {
                Value = index
            };
            var change = layerToAdd[index] * layerMultiplier;
            allBaseMarkers[voxelIndex, markerLayerIndex] = Mathf.Max(allBaseMarkers[voxelIndex, markerLayerIndex] + change, 0);
        }
    }
    [BurstCompile]
    public struct NativeArrayClearJob : IJobParallelFor
    {
        public float newValue;
        [WriteOnly]
        public NativeArray<float> writeArray;

        public void Execute(int index)
        {
            writeArray[index] = newValue;
        }
    }
}
