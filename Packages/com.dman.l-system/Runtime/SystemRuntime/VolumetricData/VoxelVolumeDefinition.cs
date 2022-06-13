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
