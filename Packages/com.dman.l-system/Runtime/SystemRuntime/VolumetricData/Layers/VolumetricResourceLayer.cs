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
            var copiedData = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.TempJob);

            var copyInJob = new CopyVoxelToWorkingDataJob
            {
                layerData = data,
                targetData = copiedData,
                layerId = voxelLayerId
            };

            dependecy = copyInJob.Schedule(copiedData.Length, 1000, dependecy);

            var resultArray = VoxelAdjacencyDiffuser.ComputeDiffusion(voxelLayout, copiedData, deltaTime, globalDiffusionConstant, ref dependecy);

            var copyBackJob = new CopyWorkingDataToVoxels
            {
                layerData = data,
                sourceData = resultArray,
                layerId = voxelLayerId
            };
            dependecy = copyBackJob.Schedule(copiedData.Length, 1000, dependecy);

            resultArray.Dispose(dependecy);

            return dependecy;
        }
    }
}