using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.ObjectSets;
using System.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    public struct VoxelDiffusionJob : IJob
    {
        [NativeDisableContainerSafetyRestriction]
        public VoxelWorldVolumetricLayerData layerData;
        public int layerId;
        public float deltaTime;

        public void Execute()
        {
            /// TODO
        }
    }
}