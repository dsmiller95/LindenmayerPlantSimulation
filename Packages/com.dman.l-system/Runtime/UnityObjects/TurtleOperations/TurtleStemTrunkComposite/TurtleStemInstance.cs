using Dman.LSystem.SystemRuntime.NativeCollections;
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
        /// <summary>
        /// floating point identity. a uint value of 0, or a color of RGBA(0,0,0,0) indicates there is no organ identity
        /// </summary>
        public UIntFloatColor32 organIdentity;
    }
}
