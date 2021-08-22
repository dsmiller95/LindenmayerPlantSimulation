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
        public NativeArray<float> allBaseMarkers;

        public int totalLayersInBase;
        public int markerLayerIndex;

        public void Execute(int voxelIndex)
        {
            var targetIndex = voxelIndex * totalLayersInBase + markerLayerIndex;
            allBaseMarkers[targetIndex] = Mathf.Max(allBaseMarkers[targetIndex] + newMarkerLevels[voxelIndex] - oldMarkerLevels[voxelIndex], 0);
        }
    }
    public struct NativeArraySubtractNegativeProtectionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> markerLevelsToRemove;

        [NativeDisableParallelForRestriction]
        public NativeArray<float> allBaseMarkers;
        public int totalLayersInBase;
        public int markerLayerIndex;

        public void Execute(int voxelIndex)
        {
            var targetIndex = voxelIndex * totalLayersInBase + markerLayerIndex;
            allBaseMarkers[targetIndex] = Mathf.Max(allBaseMarkers[targetIndex] - markerLevelsToRemove[voxelIndex], 0);
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
