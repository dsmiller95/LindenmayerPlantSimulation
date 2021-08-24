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
        public static VoxelIndex Invalid => new VoxelIndex(-1);
        public static VoxelIndex Zero => new VoxelIndex(0);
        public VoxelIndex(int value)
        {
            this.Value = value;
        }
    }
    /// <summary>
    /// index of a tile inside a surface representation of the voxel world
    /// </summary>
    public struct TileIndex
    {
        public int Value;
        public bool IsValid => Value >= 0;

        public static TileIndex Invalid => new TileIndex(-1);
        public static TileIndex Zero => new TileIndex(0);

        public TileIndex(int value)
        {
            this.Value = value;
        }
    }
}