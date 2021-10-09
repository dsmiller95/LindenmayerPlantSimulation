using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    /// <summary>
    /// A handle used to write to any layer via a modification buffer, and
    ///     write more quickly to one specific layer through data replication
    /// </summary>
    public struct DoubleBufferNativeWritableHandle
    {
        public NativeArray<float> targetData;

        public VolumetricWorldVoxelLayout voxelLayout;
        public Matrix4x4 localToWorldTransformation;

        public DoubleBufferNativeWritableHandle(
            NativeArray<float> targetDurabilityData,
            VolumetricWorldVoxelLayout voxelDistribution, 
            Matrix4x4 localToWorld)
        {
            this.targetData = targetDurabilityData;

            this.voxelLayout = voxelDistribution;
            this.localToWorldTransformation = localToWorld;
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
