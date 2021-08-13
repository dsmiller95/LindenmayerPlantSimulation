using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public struct VolumetricWorldVoxelLayout
    {
        public Vector3 voxelOrigin;
        public Vector3 worldSize;
        public Vector3Int worldResolution;

        public Vector3 voxelSize => new Vector3(worldSize.x / worldResolution.x, worldSize.y / worldResolution.y, worldSize.z / worldResolution.z);

        public int totalVolumeDataSize => worldResolution.x * worldResolution.y * worldResolution.z;
        public int totalSurfaceDataSize => worldResolution.x * worldResolution.z;

        public int GetDataIndexFromWorldPosition(Vector3 worldPosition)
        {
            return GetDataIndexFromCoordinates(GetVoxelCoordinates(worldPosition));
        }

        public Vector3 GetWorldPositionFromDataIndex(int dataIndex)
        {
            return CoordinateToCenterOfVoxel(GetCoordinatesFromDataIndex(dataIndex));
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

        public int GetDataIndexFromCoordinates(Vector3Int coordiantes)
        {
            if (coordiantes.x < 0 || coordiantes.x >= worldResolution.x ||
                coordiantes.y < 0 || coordiantes.y >= worldResolution.y ||
                coordiantes.z < 0 || coordiantes.z >= worldResolution.z)
            {
                return -1;
            }
            return coordiantes.x * worldResolution.y * worldResolution.z + coordiantes.y * worldResolution.z + coordiantes.z;
        }

        public Vector3 CoordinateToCenterOfVoxel(Vector3Int coordinate)
        {
            return Vector3.Scale(voxelSize, coordinate) + (voxelOrigin + (voxelSize / 2f));
        }

        public Vector3Int GetCoordinatesFromDataIndex(int index)
        {
            var x = index / (worldResolution.y * worldResolution.z);
            var y = (index / worldResolution.z) % worldResolution.y;
            var z = index % worldResolution.z;

            return new Vector3Int(x, y, z);
        }



        public int SurfaceGetDataIndexFromWorldPosition(Vector3 worldPosition)
        {
            return SurfaceGetDataIndexFromCoordinates(SurfaceGetSurfaceCoordinates(worldPosition));
        }
        public Vector2 SurfaceGetTilePositionFromDataIndex(int dataIndex)
        {
            return SurfaceToCenterOfTile(SurfaceGetCoordinatesFromDataIndex(dataIndex));
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

        public int SurfaceGetDataIndexFromCoordinates(Vector2Int coordiantes)
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

        public Vector2Int SurfaceGetCoordinatesFromDataIndex(int index)
        {
            var x = index / worldResolution.z;
            var y = index % worldResolution.z;

            return new Vector2Int(x, y);
        }
    }
}
