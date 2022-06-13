using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    /// <summary>
    /// A handle used to write to any layer via a modification buffer
    /// </summary>
    public struct CommandBufferNativeWritableHandle
    {
        public NativeList<LayerModificationCommand> modificationCommandBuffer;

        private VoxelVolume volume;
        public Matrix4x4 localToWorldTransformation;

        public CommandBufferNativeWritableHandle(
            NativeList<LayerModificationCommand> modificationCommandBuffer,
            VoxelVolume voxelDistribution,
            Matrix4x4 localToWorld)
        {
            this.modificationCommandBuffer = modificationCommandBuffer;

            this.volume = voxelDistribution;
            this.localToWorldTransformation = localToWorld;
        }
        public static CommandBufferNativeWritableHandle GetTemp(Allocator allocator = Allocator.TempJob)
        {
            return new CommandBufferNativeWritableHandle
            {
                modificationCommandBuffer = new NativeList<LayerModificationCommand>(0, allocator),
                volume = default,
                localToWorldTransformation = default
            };
        }

        public VoxelIndex GetVoxelIndexFromLocalSpace(Vector3 localPosition)
        {
            var worldPosition = localToWorldTransformation.MultiplyPoint(localPosition);
            return volume.GetVoxelIndexFromWorldPosition(worldPosition);
        }
        public void AppendAmountChangeToOtherLayer(Vector3 localPosition, float change, int layerIndex)
        {
            var voxelIndex = GetVoxelIndexFromLocalSpace(localPosition);
            if (!voxelIndex.IsValid)
            {
                return;
            }
            AppendAmountChangeToOtherLayer(voxelIndex, change, layerIndex);
        }
        public void AppendAmountChangeToOtherLayer(VoxelIndex voxelIndex, float change, int layerIndex)
        {
            modificationCommandBuffer.Add(new LayerModificationCommand
            {
                layerIndex = layerIndex,
                voxel = voxelIndex,
                valueChange = change
            });
        }
    }
}
