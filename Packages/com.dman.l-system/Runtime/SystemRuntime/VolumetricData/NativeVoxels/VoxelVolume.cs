using Dman.Utilities.SerializableUnityObjects;
using System;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels
{
    public struct VoxelVolume
    {
        public Vector3 voxelOrigin;
        public Vector3 worldSize;
        public Vector3Int worldResolution;
        public Vector3 voxelSize => new Vector3(worldSize.x / worldResolution.x, worldSize.y / worldResolution.y, worldSize.z / worldResolution.z);
        public int totalVoxels => worldResolution.x * worldResolution.y * worldResolution.z;
        public int totalTiles => worldResolution.x * worldResolution.z;


        public VoxelIndex GetVoxelIndexFromWorldPosition(Vector3 worldPosition)
        {
            return GetVoxelIndexFromVoxelCoordinates(GetVoxelCoordinatesFromWorldPosition(worldPosition));
        }

        public Vector3 GetWorldPositionFromVoxelIndex(VoxelIndex voxelIndex)
        {
            return GetWorldPositionFromVoxelCoordinates(GetVoxelCoordinatesFromVoxelIndex(voxelIndex));
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

        public Vector3Int GetVoxelCoordinatesFromWorldPosition(Vector3 worldPosition)
        {
            var relativePos = (worldPosition - voxelOrigin);

            var coord = new Vector3Int(
                Mathf.FloorToInt(relativePos.x * worldResolution.x / worldSize.x),
                Mathf.FloorToInt(relativePos.y * worldResolution.y / worldSize.y),
                Mathf.FloorToInt(relativePos.z * worldResolution.z / worldSize.z)
                );

            return coord;
        }

        public Vector3 GetWorldPositionFromVoxelCoordinates(Vector3Int coordinate)
        {
            return Vector3.Scale(voxelSize, coordinate) + (voxelOrigin + (voxelSize / 2f));
        }



        public TileIndex SurfaceGetTileIndexFromWorldPosition(Vector3 worldPosition)
        {
            return SurfaceGetTileIndexFromTileCoordinates(SurfaceGetTileCoordinatesFromWorldPosition(worldPosition));
        }
        public Vector2 SurfaceGetTilePositionFromTileIndex(TileIndex tileIndex)
        {
            return SurfaceGetTilePositionFromTileCoordinates(SurfaceGetTileCoordinatesFromTileIndex(tileIndex));
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

        public Vector2Int SurfaceGetTileCoordinatesFromWorldPosition(Vector3 worldPosition)
        {
            var relativePos = (worldPosition - voxelOrigin);

            var coord = new Vector2Int(
                Mathf.FloorToInt(relativePos.x * worldResolution.x / worldSize.x),
                Mathf.FloorToInt(relativePos.z * worldResolution.z / worldSize.z)
                );

            return coord;
        }
        public Vector2 SurfaceGetTilePositionFromTileCoordinates(Vector2Int coordinate)
        {
            var voxelPoint = GetWorldPositionFromVoxelCoordinates(new Vector3Int(coordinate.x, 0, coordinate.y));
            return new Vector2(voxelPoint.x, voxelPoint.z);
        }
    }
}
