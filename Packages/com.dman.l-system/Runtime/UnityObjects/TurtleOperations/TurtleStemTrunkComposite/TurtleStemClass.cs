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
        public bool flipUvs;
        public Rect uvRect;

        public bool Equals(TurtleStemClass other)
        {
            return materialIndex == other.materialIndex &&
                radialResolution == other.radialResolution &&
                constrainUvs == other.constrainUvs &&
                flipUvs == other.flipUvs &&
                uvRect == other.uvRect;
        }

        public float MaxUvYHeight()
        {
            return uvRect.height / uvRect.width;
        }

        public float2 RemapUv(float2 uv)
        {
            if (!constrainUvs) return uv;
            var originedUvs = new float2(
                uv.x * uvRect.width,
                uv.y * uvRect.width);
            if (flipUvs)
            {
                originedUvs = new float2(originedUvs.y + (uvRect.width - uvRect.height)/2f, originedUvs.x + (uvRect.height - uvRect.width)/2f);
            }
            return originedUvs + new float2(uvRect.xMin, uvRect.yMin);
        }
    }
}
