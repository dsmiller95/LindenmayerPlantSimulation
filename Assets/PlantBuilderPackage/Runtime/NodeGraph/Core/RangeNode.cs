using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Range")] // Add the node in the node creation context menu
    public class RangeNode : BaseNode
    {
        [Input(name = "Min")]
        public float min = 1;
        [Input(name = "Max")]
        public float max = 1;
        [Output(name = "Out")]
        public float output;

        public override string name => "Range";

        protected override void Process()
        {
            var typedGraph = graph as PlantMeshGeneratorGraph;
            if(graph == null)
            {
                Debug.LogWarning("owner graph isn't our kin");
                return;
            }
            output = (float)(typedGraph.MyRandom.NextDouble() * (max - min) + min);
        }
    }
}