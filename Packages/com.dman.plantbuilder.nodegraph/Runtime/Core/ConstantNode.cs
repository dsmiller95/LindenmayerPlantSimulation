using GraphProcessor;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Constant")]
    public class ConstantNode : BaseNode
    {
        [Output(name = "Out")]
        public float output;
        public override string name => "Constant";
    }
}
