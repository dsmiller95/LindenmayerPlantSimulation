using Dman.Utilities.SerializableUnityObjects;
using System;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels
{
    public struct VolumetricWorldVoxelLayout
    {
        public VoxelVolume volume;
        private Vector3Int worldResolution => volume.worldResolution;
        public int dataLayerCount;

        public int totalDataSize => volume.totalVoxels * dataLayerCount;


        public int GetDataIndexForLayerAtVoxel(int voxelIndex, int layer)
        {
            return voxelIndex * dataLayerCount + layer;
        }

        public VoxelIndex GetVoxelIndexFromWorldPosition(Vector3 worldPosition)
        {
            return GetVoxelIndexFromVoxelCoordinates(volume.GetVoxelCoordinatesFromWorldPosition(worldPosition));
        }

        public Vector3 GetWorldPositionFromVoxelIndex(VoxelIndex voxelIndex)
        {
            return volume.GetWorldPositionFromVoxelCoordinates(GetVoxelCoordinatesFromVoxelIndex(voxelIndex));
        }

        public VoxelIndex GetVoxelIndexFromVoxelCoordinates(int x, int y, int z)
        {
            if (x < 0 || x >= worldResolution.x ||
                y < 0 || y >= worldResolution.y ||
                z < 0 || z >= worldResolution.z)
            {
                return new VoxelIndex
                {
                    Value = -1
                };
            }
            return new VoxelIndex
            {
                Value = (x * worldResolution.y + y) * worldResolution.z + z
            };
        }

        public VoxelIndex GetVoxelIndexFromVoxelCoordinates(Vector3Int coordiantes)
        {
            return GetVoxelIndexFromVoxelCoordinates(coordiantes.x, coordiantes.y, coordiantes.z);
        }

        public Vector3Int GetVoxelCoordinatesFromVoxelIndex(VoxelIndex voxelIndex)
        {
            var x = voxelIndex.Value / (worldResolution.y * worldResolution.z);
            var y = (voxelIndex.Value / worldResolution.z) % worldResolution.y;
            var z = voxelIndex.Value % worldResolution.z;

            return new Vector3Int(x, y, z);
        }

        public TileIndex SurfaceGetTileIndexFromWorldPosition(Vector3 worldPosition)
        {
            return SurfaceGetTileIndexFromTileCoordinates(volume.SurfaceGetTileCoordinatesFromWorldPosition(worldPosition));
        }
        public Vector2 SurfaceGetTilePositionFromTileIndex(TileIndex tileIndex)
        {
            return volume.SurfaceGetTilePositionFromTileCoordinates(SurfaceGetTileCoordinatesFromTileIndex(tileIndex));
        }


        public TileIndex SurfaceGetTileIndexFromTileCoordinates(Vector2Int coordiantes)
        {
            if (coordiantes.x < 0 || coordiantes.x >= worldResolution.x ||
                coordiantes.y < 0 || coordiantes.y >= worldResolution.z)
            {
                return new TileIndex
                {
                    Value = -1
                };
            }
            return new TileIndex
            {
                Value = coordiantes.x * worldResolution.z + coordiantes.y
            };
        }

        public Vector2Int SurfaceGetTileCoordinatesFromTileIndex(TileIndex tileIndex)
        {
            var x = tileIndex.Value / worldResolution.z;
            var y = tileIndex.Value % worldResolution.z;

            return new Vector2Int(x, y);
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
