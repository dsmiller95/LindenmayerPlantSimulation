using Dman.Math;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    /// <summary>
    /// data pulled from the turtle reading job directly. should be as lean as possible,
    ///     only include info which absolutely must be queried or computed as part of 
    ///     the primary turtle job
    /// </summary>
    public struct TurtleStemInstance
    {
        public ushort stemClassIndex;
        public Matrix4x4 orientation;
        public int parentIndex;
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
