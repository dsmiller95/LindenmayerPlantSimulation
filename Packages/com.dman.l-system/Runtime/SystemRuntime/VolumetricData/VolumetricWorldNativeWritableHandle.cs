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
        public NativeArray<float> target;
        public VolumetricWorldVoxelLayout voxelLayout;
        public Matrix4x4 localToWorldTransformation;

        public VolumetricWorldNativeWritableHandle(NativeArray<float> target, VolumetricWorldVoxelLayout voxelDistribution, Matrix4x4 localToWorld)
        {
            this.target = target;
            this.voxelLayout = voxelDistribution;
            this.localToWorldTransformation = localToWorld;
        }


        public void WriteVolumetricAmountToTarget(float amount, Vector3 localPosition)
        {
            var worldPosition = localToWorldTransformation.MultiplyPoint(localPosition);
            var voxelPoint = voxelLayout.GetVoxelCoordinates(worldPosition);
            var indexInTarget = voxelLayout.GetDataIndexFromCoordinates(voxelPoint);
            if(indexInTarget < 0)
            {
                return;
            }
            target[indexInTarget] += amount;
        }
    }
}
