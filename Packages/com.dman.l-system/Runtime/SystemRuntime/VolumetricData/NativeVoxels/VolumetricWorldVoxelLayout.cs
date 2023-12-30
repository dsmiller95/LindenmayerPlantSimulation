using Dman.Utilities.SerializableUnityObjects;
using System;

namespace Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels
{
    public struct VolumetricWorldVoxelLayout
    {
        public VoxelVolume volume;
        public int dataLayerCount;

        public int totalDataSize => volume.totalVoxels * dataLayerCount;


        public int GetDataIndexForLayerAtVoxel(int voxelIndex, int layer)
        {
            return voxelIndex * dataLayerCount + layer;
        }


        [Serializable]
        public class Serializable
        {
            public SerializableVector3 origin;
            public SerializableVector3 size;
            public SerializableVector3Int resolution;
            public int dataLayerCount;
            public Serializable(VolumetricWorldVoxelLayout source)
            {
                origin = source.volume.voxelOrigin;
                size = source.volume.voxelSize;
                resolution = source.volume.worldResolution;
                dataLayerCount = source.dataLayerCount;
            }

            public VolumetricWorldVoxelLayout Deserialize()
            {
                return new VolumetricWorldVoxelLayout
                {
                    volume = new VoxelVolume
                    {
                        voxelOrigin = origin,
                        worldSize = size,
                        worldResolution = resolution
                    },
                    dataLayerCount = dataLayerCount
                };
            }
        }
    }
}
