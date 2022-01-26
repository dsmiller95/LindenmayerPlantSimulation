using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    [CreateAssetMenu(fileName = "VolumetricResourceLayer", menuName = "LSystem/Resource Layers/VolumetricResourceLayer")]
    public class VolumetricResourceLayer : ScriptableObject
    {
        public string description;

        public VolumetricLayerEffect[] effects;
        [NonSerialized]
        public int voxelLayerId;

        public virtual void SetupInternalData(VolumetricWorldVoxelLayout layout, int myLayerId)
        {
            voxelLayerId = myLayerId;
            foreach (var effect in effects)
            {
                effect.SetupInternalData(layout);
            }
        }
        public virtual void CleanupInternalData(VolumetricWorldVoxelLayout layout)
        {
            foreach (var effect in effects)
            {
                effect.CleanupInternalData(layout);
            }
        }

        public virtual bool ApplyLayerWideUpdate(VoxelWorldVolumetricLayerData data, float deltaTime, ref JobHandleWrapper dependecy)
        {
            if (effects.Length <= 0)
            {
                return false;
            }


            var voxelLayout = data.VoxelLayout;
            var copyInData = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.TempJob);

            var copyInJob = new CopyVoxelToWorkingDataJob
            {
                layerData = data,
                targetData = copyInData,
                layerId = voxelLayerId
            };

            dependecy = copyInJob.Schedule(copyInData.Length, 1000, dependecy);

            var swapSpace = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.TempJob);
            var workingData = new DoubleBuffered<float>(copyInData, swapSpace);

            var changed = false;
            foreach (var effect in effects)
            {
                if (effect.ApplyEffectToLayer(workingData, data, deltaTime, ref dependecy))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                var copyBackJob = new CopyWorkingDataToVoxels
                {
                    layerData = data,
                    sourceData = workingData.CurrentData,
                    layerId = this.voxelLayerId
                };
                dependecy = copyBackJob.Schedule(workingData.CurrentData.Length, 1000, dependecy);
                workingData.Dispose(dependecy);
            }

            return changed;
        }
    }
}