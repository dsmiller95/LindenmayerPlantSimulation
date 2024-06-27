using Dman.Math;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct TurtleState
    {
        public static readonly TurtleState DEFAULT = new TurtleState
        {
            transformation = Matrix4x4.identity,
            thickness = 1,
            organIdentity = new UIntFloatColor32(0),
            indexInStemTree = -1
        };

        public Matrix4x4 transformation;
        public float thickness;
        public UIntFloatColor32 organIdentity;
        public byte4 customData;
        public int indexInStemTree;
    }
}
