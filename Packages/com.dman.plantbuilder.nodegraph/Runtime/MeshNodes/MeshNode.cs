using Dman.MeshDraftExtensions;
using GraphProcessor;
using System;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [Serializable]
    public abstract class MeshNode : BaseNode
    {
        [Output(name = "Out")]
        public MeshDraftWithExtras output;
    }
}
