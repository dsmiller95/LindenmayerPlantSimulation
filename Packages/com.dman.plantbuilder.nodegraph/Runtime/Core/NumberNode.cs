using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Number")] // Add the node in the node creation context menu
    public class NumberNode : BaseNode
    {
        [Input(name = "In")]
        public float input = 1;
        [Output(name = "Out")]
        public float output;

        public override string name => "Number";

        protected override void Process()
        {
            output = input;// new DeferredConstantEvaluator<float>(input);
        }
    }
}