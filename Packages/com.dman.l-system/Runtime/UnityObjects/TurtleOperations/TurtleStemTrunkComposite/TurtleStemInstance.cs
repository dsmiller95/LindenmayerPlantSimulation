using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.Utilities.Math;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct TurtleStemInstance
    {
        public byte materialIndex;
        public ushort radialResolution;
        public Matrix4x4 orientation;
        public int parentIndex;
        public int depth;
        /// <summary>
        /// floating point identity. a uint value of 0, or a color of RGBA(0,0,0,0) indicates there is no organ identity
        /// </summary>
        public UIntFloatColor32 organIdentity;
        /// <summary>
        /// generic extra vertex data
        /// </summary>
        public byte4 extraData;
    }
}
