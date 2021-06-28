using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct TurtleState
    {
        public int submeshIndex; // material index
        public Matrix4x4 transformation;
        public float thickness;
        public UIntFloatColor32 organIdentity;
    }
}
