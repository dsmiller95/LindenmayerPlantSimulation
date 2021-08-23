using System;
using System.Collections;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    /// <summary>
    /// index of a voxel inside a 3D volume
    /// </summary>
    public struct VoxelIndex
    {
        public int Value;

        public bool IsValid => Value >= 0;

        public static implicit operator VoxelIndex(int v)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// index of a tile inside a surface representation of the voxel world
    /// </summary>
    public struct TileIndex
    {
        public int Value;
        public bool IsValid => Value >= 0;
    }
}