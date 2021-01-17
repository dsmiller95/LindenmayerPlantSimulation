using GraphProcessor;
using PlantBuilder.NodeGraph.MeshNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [Serializable]
    public abstract class MeshNode: BaseNode
    {
        [Output(name = "Out")]
        public MeshDraftWithExtras output;
    }
}
