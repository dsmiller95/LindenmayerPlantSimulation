using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.Utilities.Math;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    /// <summary>
    /// data pulled from the turtle reading job directly. should be as lean as possible,
    ///     only include info which absolutely must be queried or computed as part of 
    ///     the primary turtle job
    /// </summary>
    public struct TurtleStemClass : IEquatable<TurtleStemClass>
    {
        public byte materialIndex;
        public ushort radialResolution;
        public bool constrainUvs;
        public Rect uvRect;

        public bool Equals(TurtleStemClass other)
        {
            return materialIndex == other.materialIndex &&
                radialResolution == other.radialResolution &&
                constrainUvs == other.constrainUvs &&
                uvRect == other.uvRect;
        }
    }
}
