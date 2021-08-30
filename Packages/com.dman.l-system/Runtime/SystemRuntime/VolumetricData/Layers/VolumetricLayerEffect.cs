using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.ObjectSets;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    public abstract class VolumetricLayerEffect : ScriptableObject
    {
        public string description;

        public virtual void SetupInternalData(VolumetricWorldVoxelLayout layout)
        {
        }
        public virtual void CleanupInternalData(VolumetricWorldVoxelLayout layout)
        {
        }

        /// <summary>
        /// apply whatever effect this transform applies to the layer. 
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="deltaTime"></param>
        /// <param name="dependecy"></param>
        /// <returns>true if any changes were applied</returns>
        public abstract bool ApplyEffectToLayer(DoubleBuffered<float> layerData, VolumetricWorldVoxelLayout layout, float deltaTime, ref JobHandle dependecy);
    }
}