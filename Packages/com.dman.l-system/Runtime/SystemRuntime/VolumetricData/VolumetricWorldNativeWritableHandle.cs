using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public struct VolumetricWorldNativeWritableHandle
    {
        public NativeArray<float> targetDurabilityData;
        public NativeList<LayerModificationCommand> modificationCommandBuffer;

        public VolumetricWorldVoxelLayout voxelLayout;
        public Matrix4x4 localToWorldTransformation;

        public VolumetricWorldNativeWritableHandle(
            NativeArray<float> targetDurabilityData,
            NativeList<LayerModificationCommand> modificationCommandBuffer,
            VolumetricWorldVoxelLayout voxelDistribution, 
            Matrix4x4 localToWorld)
        {
            this.targetDurabilityData = targetDurabilityData;
            this.modificationCommandBuffer = modificationCommandBuffer;

            this.voxelLayout = voxelDistribution;
            this.localToWorldTransformation = localToWorld;
        }

        public int GetVoxelIndexFromLocalSpace(Vector3 localPosition)
        {
            var worldPosition = localToWorldTransformation.MultiplyPoint(localPosition);
            return voxelLayout.GetVoxelIndexFromWorldPosition(worldPosition);
        }
        public void AppentAmountChangeToLayer(Vector3 localPosition, float change, int layerIndex)
        {
            var voxelIndex = GetVoxelIndexFromLocalSpace(localPosition);
            if (voxelIndex < 0)
            {
                return;
            }
            var resourceLayerIndex = voxelIndex * voxelLayout.dataLayerCount + layerIndex;
            modificationCommandBuffer.Add(new LayerModificationCommand
            {
                resourceLayerIndex = resourceLayerIndex,
                valueChange = change
            });
        }

        public void WriteVolumetricDurabilityToTarget(float amount, Vector3 localPosition)
        {
            var indexInTarget = GetVoxelIndexFromLocalSpace(localPosition);
            if(indexInTarget < 0)
            {
                return;
            }
            targetDurabilityData[indexInTarget] += amount;
        }
    }
}
