using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    public struct VoxelKernelComputeJob : IJob
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

    public struct VoxelKernelDiffusionJob : IJobParallelFor
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
            if((oneDimensionalKernel.Length - 1) % 2 != 0)
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
            var rootCoordiante = voxelLayout.GetCoordinatesFromVoxelIndex(voxelIndex);

            var kernelOrigin = oneDimensionalKernel.Length / 2;
            float newValue = 0;
            for (int kernelIndex = 0; kernelIndex < oneDimensionalKernel.Length; kernelIndex++)
            {
                var offset = axisVector * (kernelIndex - kernelOrigin);
                var kernelWeight = oneDimensionalKernel[kernelIndex];

                var sampleCoordinate = offset + rootCoordiante;
                var sampleIndex = voxelLayout.GetVoxelIndexFromCoordinates(sampleCoordinate);

                float sampleValue;
                if (!sampleIndex.IsValid)
                {
                    sampleValue = boundaryValue;
                }else
                {
                    sampleValue = sourceDiffusionValues[sampleIndex.Value];
                }
                newValue += sampleValue * kernelWeight;
            }

            targetDiffusionValues[voxelIndex.Value] = newValue;
        }
    }
};