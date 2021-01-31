using UnityEngine;

namespace Dman.LSystem
{
    public struct TurtleMeshState<T> where T: struct
    {
        public int submeshIndex;
        public T turtleBaseState;
        public TurtleMeshState(T defaultState)
        {
            this.turtleBaseState = defaultState;
            submeshIndex = 0;
        }
    }

    public struct TurtleState
    {
        public Matrix4x4 transformation;
    }
}
