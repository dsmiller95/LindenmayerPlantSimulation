using Dman.Utilities.Math;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct TurtleState
    {
        public Matrix4x4 transformation;
        public float thickness;
        public UIntFloatColor32 organIdentity;
        public byte4 customData;
    }
}
