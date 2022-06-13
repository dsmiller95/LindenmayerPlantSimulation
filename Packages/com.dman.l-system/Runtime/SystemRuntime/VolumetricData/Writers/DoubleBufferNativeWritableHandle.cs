using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    /// <summary>
    /// A handle used to write to any layer via a modification buffer, and
    ///     write more quickly to one specific layer through data replication
    /// </summary>
    public struct DoubleBufferNativeWritableHandle
    {
        public NativeArray<float> targetData;

        private VoxelVolume voxelLayout;
        public Matrix4x4 localToWorldTransformation;

        public DoubleBufferNativeWritableHandle(
            NativeArray<float> targetDurabilityData,
            VoxelVolume voxelDistribution,
            Matrix4x4 localToWorld)
        {
            this.targetData = targetDurabilityData;

            this.voxelLayout = voxelDistribution;
            this.localToWorldTransformation = localToWorld;
        }

        public static DoubleBufferNativeWritableHandle GetTemp(Allocator allocator = Allocator.TempJob)
        {
            return new DoubleBufferNativeWritableHandle
            {
                targetData = new NativeArray<float>(0, allocator),
                voxelLayout = default,
                localToWorldTransformation = default
            };
        }

        public VoxelIndex GetVoxelIndexFromLocalSpace(Vector3 localPosition)
        {
            var worldPosition = localToWorldTransformation.MultiplyPoint(localPosition);
            return voxelLayout.GetVoxelIndexFromWorldPosition(worldPosition);
        }

        public void WriteVolumetricAmountToDoubleBufferedData(float amount, Vector3 localPosition)
        {
            var voxelIndex = GetVoxelIndexFromLocalSpace(localPosition);
            if (!voxelIndex.IsValid)
            {
                return;
            }
            targetData[voxelIndex.Value] += amount;
        }
    }
}
