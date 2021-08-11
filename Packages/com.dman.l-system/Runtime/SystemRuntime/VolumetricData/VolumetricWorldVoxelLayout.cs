using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public struct VolumetricWorldVoxelLayout
    {
        public Vector3 voxelOrigin;
        public Vector3 worldSize;
        public Vector3Int worldResolution;

        public int totalVolumeDataSize => worldResolution.x * worldResolution.y * worldResolution.z;

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
    }
}
