using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.ObjectSets;
using System.Collections;
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

        public JobHandle ApplyLayerWideUpdate(VoxelWorldVolumetricLayerData data, JobHandle dependecy)
        {
            if (diffuse)
            {
                var diffuseJob = new VoxelDiffusionJob
                {
                    layerData = data,
                    layerId = voxelLayerId,
                    deltaTime = Time.deltaTime
                };
                dependecy = diffuseJob.Schedule(dependecy);
            }
            return dependecy;
        }
    }
}