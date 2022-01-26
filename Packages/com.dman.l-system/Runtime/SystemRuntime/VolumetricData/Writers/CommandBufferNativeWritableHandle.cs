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

        public VolumetricWorldVoxelLayout voxelLayout;
        public Matrix4x4 localToWorldTransformation;

        public CommandBufferNativeWritableHandle(
            NativeList<LayerModificationCommand> modificationCommandBuffer,
            VolumetricWorldVoxelLayout voxelDistribution,
            Matrix4x4 localToWorld)
        {
            this.modificationCommandBuffer = modificationCommandBuffer;

            this.voxelLayout = voxelDistribution;
            this.localToWorldTransformation = localToWorld;
        }

        public VoxelIndex GetVoxelIndexFromLocalSpace(Vector3 localPosition)
        {
            var worldPosition = localToWorldTransformation.MultiplyPoint(localPosition);
            return voxelLayout.GetVoxelIndexFromWorldPosition(worldPosition);
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
