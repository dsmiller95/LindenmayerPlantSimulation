using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
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

        public int GetVoxelIndexFromWorldPosition(Vector3 worldPosition)
        {
            return GetVoxelIndexFromCoordinates(GetVoxelCoordinates(worldPosition));
        }

        public Vector3 GetWorldPositionFromVoxelIndex(int voxelIndex)
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

        public int GetVoxelIndexFromCoordinates(Vector3Int coordiantes)
        {
            if (coordiantes.x < 0 || coordiantes.x >= worldResolution.x ||
                coordiantes.y < 0 || coordiantes.y >= worldResolution.y ||
                coordiantes.z < 0 || coordiantes.z >= worldResolution.z)
            {
                return -1;
            }
            return (coordiantes.x * worldResolution.y + coordiantes.y) * worldResolution.z + coordiantes.z;
        }

        public Vector3 CoordinateToCenterOfVoxel(Vector3Int coordinate)
        {
            return Vector3.Scale(voxelSize, coordinate) + (voxelOrigin + (voxelSize / 2f));
        }

        public Vector3Int GetCoordinatesFromVoxelIndex(int voxelIndex)
        {
            var x = voxelIndex / (worldResolution.y * worldResolution.z);
            var y = (voxelIndex / worldResolution.z) % worldResolution.y;
            var z = voxelIndex % worldResolution.z;

            return new Vector3Int(x, y, z);
        }

        public int SurfaceGetTileIndexFromWorldPosition(Vector3 worldPosition)
        {
            return SurfaceGetTileIndexFromCoordinates(SurfaceGetSurfaceCoordinates(worldPosition));
        }
        public Vector2 SurfaceGetTilePositionFromTileIndex(int tileIndex)
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

        public int SurfaceGetTileIndexFromCoordinates(Vector2Int coordiantes)
        {
            if (coordiantes.x < 0 || coordiantes.x >= worldResolution.x ||
                coordiantes.y < 0 || coordiantes.y >= worldResolution.z)
            {
                return -1;
            }
            return coordiantes.x * worldResolution.z + coordiantes.y;
        }

        public Vector2 SurfaceToCenterOfTile(Vector2Int coordinate)
        {
            var voxelPoint = CoordinateToCenterOfVoxel(new Vector3Int(coordinate.x, 0, coordinate.y));
            return new Vector2(voxelPoint.x, voxelPoint.z);
        }

        public Vector2Int SurfaceGetCoordinatesFromTileIndex(int tileIndex)
        {
            var x = tileIndex / worldResolution.z;
            var y = tileIndex % worldResolution.z;

            return new Vector2Int(x, y);
        }
    }
}
