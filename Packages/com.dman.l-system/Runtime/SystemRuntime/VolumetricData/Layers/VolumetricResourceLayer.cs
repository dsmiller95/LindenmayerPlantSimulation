using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.ObjectSets;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    [CreateAssetMenu(fileName = "VolumetricResourceLayer", menuName = "LSystem/VolumetricResourceLayer")]
    public class VolumetricResourceLayer : ScriptableObject
    {
        public int voxelLayerId;
        public string description;

        public bool diffuse;
        public float globalDiffusionConstant = 1;

        public JobHandle ApplyLayerWideUpdate(VoxelWorldVolumetricLayerData data, float deltaTime, JobHandle dependecy)
        {
            if (diffuse)
            {
                dependecy = this.Diffuse(data, deltaTime, dependecy);
            }
            return dependecy;
        }

        private JobHandle Diffuse(VoxelWorldVolumetricLayerData data, float deltaTime, JobHandle dependecy)
        {
            var voxelLayout = data.VoxelLayout;
            var tmpDataA = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.TempJob);

            var copyInJob = new CopyVoxelToWorkingDataJob
            {
                layerData = data,
                targetData = tmpDataA,
                layerId = voxelLayerId
            };

            var copyDep = copyInJob.Schedule(tmpDataA.Length, 1000, dependecy);

            var kernel = new NativeArray<float>(3, Allocator.TempJob);
            var kernelJob = new VoxelKernelComputeJob
            {
                oneDimensionalKernel = kernel,
                deltaTime = deltaTime,
                diffusionConstant = globalDiffusionConstant
            };

            var kernelDep = kernelJob.Schedule(dependecy);
            kernelDep.Complete();

            dependecy = JobHandle.CombineDependencies(copyDep, kernelDep);

            var tmpDataB = new NativeArray<float>(data.VoxelLayout.totalVoxels, Allocator.TempJob);

            var diffuseXJob = new VoxelKernelDiffusionJob
            {
                sourceDiffusionValues = tmpDataA,
                targetDiffusionValues = tmpDataB,

                oneDimensionalKernel = kernel,

                diffuseAxis = VoxelKernelDiffusionJob.DiffusionAxis.X,
                voxelLayout = voxelLayout,
                boundaryValue = 0
            };
            dependecy = diffuseXJob.Schedule(tmpDataA.Length, 1000, dependecy);

            var diffuseYJob = new VoxelKernelDiffusionJob
            {
                sourceDiffusionValues = tmpDataB,
                targetDiffusionValues = tmpDataA,

                oneDimensionalKernel = kernel,

                diffuseAxis = VoxelKernelDiffusionJob.DiffusionAxis.Y,
                voxelLayout = voxelLayout,
                boundaryValue = 0
            };
            dependecy = diffuseYJob.Schedule(tmpDataA.Length, 1000, dependecy);

            var diffuseZJob = new VoxelKernelDiffusionJob
            {
                sourceDiffusionValues = tmpDataA,
                targetDiffusionValues = tmpDataB,

                oneDimensionalKernel = kernel,

                diffuseAxis = VoxelKernelDiffusionJob.DiffusionAxis.Z,
                voxelLayout = voxelLayout,
                boundaryValue = 0
            };
            dependecy = diffuseZJob.Schedule(tmpDataA.Length, 1000, dependecy);

            var copyBackJob = new CopyWorkingDataToVoxels
            {
                layerData = data,
                sourceData = tmpDataB,
                layerId = voxelLayerId
            };
            dependecy = copyBackJob.Schedule(tmpDataA.Length, 1000, dependecy);

            tmpDataA.Dispose(dependecy);
            tmpDataB.Dispose(dependecy);
            kernel.Dispose(dependecy);

            return dependecy;
        }
    }
}