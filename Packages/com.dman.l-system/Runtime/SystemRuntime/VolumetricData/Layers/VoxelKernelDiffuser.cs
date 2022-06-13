using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    /// <summary>
    /// this diffuser uses a kernel to perform diffusion. this may perform better for higher-fidelity diffusion
    ///     in which a wider gradient needs to be generated in only one pass
    /// </summary>
    public class VoxelKernelDiffuser
    {
        /// <summary>
        /// takes in a by-voxel data array. returns another array of the same format with the diffused results.
        ///     may modify the values in the input array. may return the input array. will handle disposing the input
        ///     array if not returned.
        /// </summary>
        public static NativeArray<float> ComputeDiffusion(
            VolumetricWorldVoxelLayout voxelLayout,
            NativeArray<float> inputArrayWithData,
            float deltaTime,
            float diffusionConstant,
            ref JobHandle dependecy)
        {
            var kernel = new NativeArray<float>(3, Allocator.TempJob);
            var kernelJob = new VoxelKernelComputeJob
            {
                oneDimensionalKernel = kernel,
                deltaTime = deltaTime,
                diffusionConstant = diffusionConstant
            };

            var kernelDep = kernelJob.Schedule();

            dependecy = JobHandle.CombineDependencies(dependecy, kernelDep);

            var tmpSwapSpace = new NativeArray<float>(voxelLayout.volume.totalVoxels, Allocator.TempJob);

            var diffuseXJob = new VoxelKernelDiffusionJob
            {
                sourceDiffusionValues = inputArrayWithData,
                targetDiffusionValues = tmpSwapSpace,

                oneDimensionalKernel = kernel,

                diffuseAxis = VoxelKernelDiffusionJob.DiffusionAxis.X,
                voxelLayout = voxelLayout,
                boundaryValue = 0
            };
            dependecy = diffuseXJob.Schedule(inputArrayWithData.Length, 1000, dependecy);

            var diffuseYJob = new VoxelKernelDiffusionJob
            {
                sourceDiffusionValues = tmpSwapSpace,
                targetDiffusionValues = inputArrayWithData,

                oneDimensionalKernel = kernel,

                diffuseAxis = VoxelKernelDiffusionJob.DiffusionAxis.Y,
                voxelLayout = voxelLayout,
                boundaryValue = 0
            };
            dependecy = diffuseYJob.Schedule(inputArrayWithData.Length, 1000, dependecy);

            var diffuseZJob = new VoxelKernelDiffusionJob
            {
                sourceDiffusionValues = inputArrayWithData,
                targetDiffusionValues = tmpSwapSpace,

                oneDimensionalKernel = kernel,

                diffuseAxis = VoxelKernelDiffusionJob.DiffusionAxis.Z,
                voxelLayout = voxelLayout,
                boundaryValue = 0
            };
            dependecy = diffuseZJob.Schedule(inputArrayWithData.Length, 1000, dependecy);


            inputArrayWithData.Dispose(dependecy);
            kernel.Dispose(dependecy);

            return tmpSwapSpace;
        }

        [BurstCompile]
        struct VoxelKernelComputeJob : IJob
        {
            public NativeArray<float> oneDimensionalKernel;
            public float deltaTime;
            public float diffusionConstant;

            public void Execute()
            {
                if ((oneDimensionalKernel.Length - 1) % 2 != 0)
                {
                    throw new System.Exception("kernel must be odd number length");
                }


                var combinedDiffusionFactor = deltaTime * diffusionConstant;
                var kernelOrigin = oneDimensionalKernel.Length / 2;
                oneDimensionalKernel[kernelOrigin] = 1;

                for (int diffuseLoop = 0; diffuseLoop < kernelOrigin * 2; diffuseLoop++)
                {
                    for (int i = 0; i < oneDimensionalKernel.Length - 1; i++)
                    {
                        var amountA = oneDimensionalKernel[i];
                        var amountB = oneDimensionalKernel[i + 1];
                        var diff = amountA - amountB;
                        var movement = diff * combinedDiffusionFactor;

                        oneDimensionalKernel[i] -= movement;
                        oneDimensionalKernel[i + 1] += movement;
                    }
                }

                // ensure symmetry
                for (int i = 0; i < kernelOrigin; i++)
                {
                    var inverseIndex = oneDimensionalKernel.Length - i - 1;
                    var total = oneDimensionalKernel[i] + oneDimensionalKernel[inverseIndex];

                    oneDimensionalKernel[i] = total / 2;
                    oneDimensionalKernel[inverseIndex] = total / 2;
                }

            }
        }

        [BurstCompile]
        struct VoxelKernelDiffusionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float> sourceDiffusionValues;
            public NativeArray<float> targetDiffusionValues;

            [ReadOnly]
            public NativeArray<float> oneDimensionalKernel;
            public DiffusionAxis diffuseAxis;
            public VolumetricWorldVoxelLayout voxelLayout;

            public float boundaryValue;

            public enum DiffusionAxis
            {
                X, Y, Z
            }

            public void Execute(int index)
            {
                if ((oneDimensionalKernel.Length - 1) % 2 != 0)
                {
                    throw new System.Exception("kernel must be odd number length");
                }

                Vector3Int axisVector;
                switch (diffuseAxis)
                {
                    case DiffusionAxis.X:
                        axisVector = new Vector3Int(1, 0, 0);
                        break;
                    case DiffusionAxis.Y:
                        axisVector = new Vector3Int(0, 1, 0);
                        break;
                    case DiffusionAxis.Z:
                        axisVector = new Vector3Int(0, 0, 1);
                        break;
                    default:
                        throw new System.Exception("must specify diffuse axis");
                }

                var voxelIndex = new VoxelIndex
                {
                    Value = index
                };
                var rootCoordiante = voxelLayout.GetVoxelCoordinatesFromVoxelIndex(voxelIndex);

                var kernelOrigin = oneDimensionalKernel.Length / 2;
                float newValue = 0;
                for (int kernelIndex = 0; kernelIndex < oneDimensionalKernel.Length; kernelIndex++)
                {
                    var offset = axisVector * (kernelIndex - kernelOrigin);
                    var kernelWeight = oneDimensionalKernel[kernelIndex];

                    var sampleCoordinate = offset + rootCoordiante;
                    var sampleIndex = voxelLayout.GetVoxelIndexFromVoxelCoordinates(sampleCoordinate);

                    float sampleValue;
                    if (!sampleIndex.IsValid)
                    {
                        sampleValue = boundaryValue;
                    }
                    else
                    {
                        sampleValue = sourceDiffusionValues[sampleIndex.Value];
                    }
                    newValue += sampleValue * kernelWeight;
                }

                targetDiffusionValues[voxelIndex.Value] = newValue;
            }
        }
    }
}
