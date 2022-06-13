using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.SceneSaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{

    public struct VoxelVolume
    {
        public Vector3 voxelOrigin;
        public Vector3 worldSize;
        public Vector3Int worldResolution;
        public Vector3 voxelSize => new Vector3(worldSize.x / worldResolution.x, worldSize.y / worldResolution.y, worldSize.z / worldResolution.z);
        public int totalVoxels => worldResolution.x * worldResolution.y * worldResolution.z;
        public int totalTiles => worldResolution.x * worldResolution.z;

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
    public class VoxelVolumeDefinition : MonoBehaviour
    {
        public Vector3 voxelOrigin => transform.position;
        public Vector3 worldSize;

        public Vector3Int worldResolution;

        public GizmoOptions gizmos = GizmoOptions.SELECTED;

        public VoxelVolume Volume => new VoxelVolume
        {
            voxelOrigin = voxelOrigin,
            worldSize = worldSize,
            worldResolution = worldResolution
        };

        private void OnDrawGizmosSelected()
        {
            if (gizmos == GizmoOptions.SELECTED)
            {
                DrawGizmos();
            }
        }
        private void OnDrawGizmos()
        {
            if (gizmos == GizmoOptions.ALWAYS)
            {
                DrawGizmos();
            }
        }

        private void DrawGizmos()
        {
            var voxelLayout = this.Volume;
            var voxelSize = voxelLayout.voxelSize;
            for (int x = 0; x < worldResolution.x; x++)
            {
                for (int y = 0; y < worldResolution.y; y++)
                {
                    for (int z = 0; z < worldResolution.z; z++)
                    {
                        var voxelCoordinate = new Vector3Int(x, y, z);
                        var cubeCenter = voxelLayout.GetWorldPositionFromVoxelCoordinates(voxelCoordinate);
                        Gizmos.color = new Color(1, 0, 0, 1);
                        Gizmos.DrawWireCube(cubeCenter, voxelSize);
                    }
                }
            }
        }
    }
}
