using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using System;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace
{
    [BurstCompile]
    public struct VoxelMarkerConsolidation : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> newMarkerLevels;
        [ReadOnly]
        public NativeArray<float> oldMarkerLevels;

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
            allBaseMarkers[voxelIndex, markerLayerIndex] = Mathf.Max(allBaseMarkers[voxelIndex, markerLayerIndex] + newMarkerLevels[index] - oldMarkerLevels[index], 0);
        }
    }
    public struct NativeArraySubtractNegativeProtectionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> markerLevelsToRemove;

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
            allBaseMarkers[voxelIndex, markerLayerIndex] = Mathf.Max(allBaseMarkers[voxelIndex, markerLayerIndex] - markerLevelsToRemove[index], 0);
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
