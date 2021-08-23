using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public struct LayerModificationCommand
    {
        public VoxelIndex voxel;
        public int layerIndex;
        public float valueChange;
    }
}
