using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Number")] // Add the node in the node creation context menu
    public class NumberNode : BaseNode
    {
        [Input(name = "In")]
        public float input = 1;
        [Output(name = "Out")]
        public DeferredEvaluator<float> output;

        public override string name => "Number";

        protected override void Process()
        {
            output = new DeferredConstantEvaluator<float>(input);
        }
    }
}