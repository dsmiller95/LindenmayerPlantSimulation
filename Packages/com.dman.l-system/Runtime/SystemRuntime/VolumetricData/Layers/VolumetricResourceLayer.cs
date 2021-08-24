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

        public virtual void SetupInternalData(VolumetricWorldVoxelLayout layout)
        {

        }
        public virtual void CleanupInternalData(VolumetricWorldVoxelLayout layout)
        {

        }

        public virtual bool ApplyLayerWideUpdate(VoxelWorldVolumetricLayerData data, float deltaTime, ref JobHandle dependecy)
        {
            var changed = false;
            if (diffuse)
            {
                dependecy = this.Diffuse(data, deltaTime, dependecy);
                changed = true;
            }
            return changed;
        }

        protected virtual JobHandle Diffuse(VoxelWorldVolumetricLayerData data, float deltaTime, JobHandle dependecy)
        {
            var voxelLayout = data.VoxelLayout;
            var diffusionData = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.TempJob);

            var copyDiffuseInJob = new CopyVoxelToWorkingDataJob
            {
                layerData = data,
                targetData = diffusionData,
                layerId = voxelLayerId
            };

            dependecy = copyDiffuseInJob.Schedule(diffusionData.Length, 1000, dependecy);

            var resultArray = VoxelAdjacencyDiffuser.ComputeDiffusion(
                voxelLayout, 
                diffusionData,
                deltaTime, 
                globalDiffusionConstant, 
                ref dependecy);

            var copyBackJob = new CopyWorkingDataToVoxels
            {
                layerData = data,
                sourceData = resultArray,
                layerId = voxelLayerId
            };
            dependecy = copyBackJob.Schedule(diffusionData.Length, 1000, dependecy);

            resultArray.Dispose(dependecy);

            return dependecy;
        }
    }
}