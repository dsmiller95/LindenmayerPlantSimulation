using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    [BurstCompile]
    public struct CopyVoxelToWorkingDataJob : IJobParallelFor
    {
        [ReadOnly]
        public VoxelWorldVolumetricLayerData layerData;
        public NativeArray<float> targetData;
        public int layerId;

        public void Execute(int index)
        {
            var voxelIndex = new VoxelIndex
            {
                Value = index
            };
            targetData[voxelIndex.Value] = layerData[voxelIndex, layerId];
        }
    }
    [BurstCompile]
    public struct CopyWorkingDataToVoxels : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction]
        public VoxelWorldVolumetricLayerData layerData;
        [ReadOnly]
        public NativeArray<float> sourceData;
        public int layerId;

        public void Execute(int index)
        {
            var voxelIndex = new VoxelIndex
            {
                Value = index
            };
            layerData[voxelIndex, layerId] = sourceData[voxelIndex.Value];
        }
    }
}