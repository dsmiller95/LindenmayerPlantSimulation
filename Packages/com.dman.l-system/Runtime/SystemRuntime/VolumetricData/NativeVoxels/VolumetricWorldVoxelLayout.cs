using Dman.Utilities.SerializableUnityObjects;
using System;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels
{
    public struct VolumetricWorldVoxelLayout
    {
        public Vector3 voxelOrigin;
        public Vector3 worldSize;
        public Vector3Int worldResolution;
        public int dataLayerCount;

        public Vector3 voxelSize => new Vector3(worldSize.x / worldResolution.x, worldSize.y / worldResolution.y, worldSize.z / worldResolution.z);

        public int totalDataSize => totalVoxels * dataLayerCount;
        public int totalVoxels => worldResolution.x * worldResolution.y * worldResolution.z;
        public int totalTiles => worldResolution.x * worldResolution.z;


        public int GetDataIndexForLayerAtVoxel(int voxelIndex, int layer)
        {
            return voxelIndex * dataLayerCount + layer;
        }

        public VoxelIndex GetVoxelIndexFromWorldPosition(Vector3 worldPosition)
        {
            return GetVoxelIndexFromCoordinates(GetVoxelCoordinates(worldPosition));
        }

        public Vector3 GetWorldPositionFromVoxelIndex(VoxelIndex voxelIndex)
        {
            return CoordinateToCenterOfVoxel(GetCoordinatesFromVoxelIndex(voxelIndex));
        }

        public Vector3Int GetVoxelCoordinates(Vector3 worldPosition)
        {
            var relativePos = (worldPosition - voxelOrigin);

            var coord = new Vector3Int(
                Mathf.FloorToInt(relativePos.x * worldResolution.x / worldSize.x),
                Mathf.FloorToInt(relativePos.y * worldResolution.y / worldSize.y),
                Mathf.FloorToInt(relativePos.z * worldResolution.z / worldSize.z)
                );

            return coord;
        }

        public VoxelIndex GetVoxelIndexFromCoordinates(int x, int y, int z)
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

        public VoxelIndex GetVoxelIndexFromCoordinates(Vector3Int coordiantes)
        {
            return GetVoxelIndexFromCoordinates(coordiantes.x, coordiantes.y, coordiantes.z);
        }

        public Vector3 CoordinateToCenterOfVoxel(Vector3Int coordinate)
        {
            return Vector3.Scale(voxelSize, coordinate) + (voxelOrigin + (voxelSize / 2f));
        }

        public Vector3Int GetCoordinatesFromVoxelIndex(VoxelIndex voxelIndex)
        {
            var x = voxelIndex.Value / (worldResolution.y * worldResolution.z);
            var y = (voxelIndex.Value / worldResolution.z) % worldResolution.y;
            var z = voxelIndex.Value % worldResolution.z;

            return new Vector3Int(x, y, z);
        }

        public TileIndex SurfaceGetTileIndexFromWorldPosition(Vector3 worldPosition)
        {
            return SurfaceGetTileIndexFromCoordinates(SurfaceGetSurfaceCoordinates(worldPosition));
        }
        public Vector2 SurfaceGetTilePositionFromTileIndex(TileIndex tileIndex)
        {
            return SurfaceToCenterOfTile(SurfaceGetCoordinatesFromTileIndex(tileIndex));
        }

        public Vector2Int SurfaceGetSurfaceCoordinates(Vector3 worldPosition)
        {
            var relativePos = (worldPosition - voxelOrigin);

            var coord = new Vector2Int(
                Mathf.FloorToInt(relativePos.x * worldResolution.x / worldSize.x),
                Mathf.FloorToInt(relativePos.z * worldResolution.z / worldSize.z)
                );

            return coord;
        }

        public TileIndex SurfaceGetTileIndexFromCoordinates(Vector2Int coordiantes)
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

        public Vector2 SurfaceToCenterOfTile(Vector2Int coordinate)
        {
            var voxelPoint = CoordinateToCenterOfVoxel(new Vector3Int(coordinate.x, 0, coordinate.y));
            return new Vector2(voxelPoint.x, voxelPoint.z);
        }

        public Vector2Int SurfaceGetCoordinatesFromTileIndex(TileIndex tileIndex)
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
                origin = source.voxelOrigin;
                size = source.voxelSize;
                resolution = source.worldResolution;
                dataLayerCount = source.dataLayerCount;
            }

            public VolumetricWorldVoxelLayout Deserialize()
            {
                return new VolumetricWorldVoxelLayout
                {
                    voxelOrigin = origin,
                    worldSize = size,
                    worldResolution = resolution,
                    dataLayerCount = dataLayerCount
                };
            }
        }
    }
}
