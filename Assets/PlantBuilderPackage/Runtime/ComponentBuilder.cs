using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder
{
    public struct NextComponentSpawnCommand
    {
        public int componentIndex;
        public Matrix4x4 componentTransformation;
    }

    public abstract class ComponentBuilder : ScriptableObject
    {
        public abstract MeshDraft CreateComponentMesh(
            Matrix4x4 meshTransform,
            int componentLevel,
            Stack<NextComponentSpawnCommand> extraComponents,
            System.Random randomGenerator);
    }
}