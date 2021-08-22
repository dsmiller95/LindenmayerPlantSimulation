using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    [BurstCompile]
    public struct LayerModificationCommandPlaybackJob : IJob
    {
        [ReadOnly]
        public NativeArray<LayerModificationCommand> commands;
        public NativeArray<float> dataArray;

        public VolumetricWorldVoxelLayout voxelLayout;

        public void Execute()
        {
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                dataArray[command.resourceLayerIndex] += command.valueChange;
            }
        }
    }
}
