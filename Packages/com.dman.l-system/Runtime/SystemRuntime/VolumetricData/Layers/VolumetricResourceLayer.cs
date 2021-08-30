using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.ObjectSets;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    [CreateAssetMenu(fileName = "VolumetricResourceLayer", menuName = "LSystem/Resource Layers/VolumetricResourceLayer")]
    public class VolumetricResourceLayer : ScriptableObject
    {
        public int voxelLayerId;
        public string description;

        public VolumetricLayerEffect[] effects;

        public virtual void SetupInternalData(VolumetricWorldVoxelLayout layout)
        {

        }
        public virtual void CleanupInternalData(VolumetricWorldVoxelLayout layout)
        {

        }

        public virtual bool ApplyLayerWideUpdate(VoxelWorldVolumetricLayerData data, float deltaTime, ref JobHandle dependecy)
        {
            var changed = false;
            foreach (var effect in effects)
            {
                if(effect.ApplyEffectToLayer(data, voxelLayerId, deltaTime, ref dependecy))
                {
                    changed = true;
                }
            }
            return changed;
        }
    }
}