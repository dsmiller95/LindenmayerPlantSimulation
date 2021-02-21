using GraphProcessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
