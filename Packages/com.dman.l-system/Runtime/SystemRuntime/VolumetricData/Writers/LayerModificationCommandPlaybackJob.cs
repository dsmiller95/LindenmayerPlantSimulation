using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    [BurstCompile]
    public struct LayerModificationCommandPlaybackJob : IJob
    {
        [ReadOnly]
        public NativeArray<LayerModificationCommand> commands;
        public VoxelWorldVolumetricLayerData dataArray;

        public void Execute()
        {
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                dataArray[command.voxel, command.layerIndex] += command.valueChange;
            }
        }
    }
}
