using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Range")] // Add the node in the node creation context menu
    public class RangeNode : BaseNode
    {
        [Input(name = "Min")]
        public DeferredEvaluator<float> min = 1;
        [Input(name = "Max")]
        public DeferredEvaluator<float> max = 2;
        [Output(name = "Out")]
        public DeferredEvaluator<float> output;

        public override string name => "Range";

        protected override void Process()
        {
            output = new DeferredRangeGenerator(this);
        }

        [System.Serializable]
        class DeferredRangeGenerator : DeferredEvaluator<float>
        {
            private DeferredEvaluator<float> min;
            private DeferredEvaluator<float> max;

            public DeferredRangeGenerator(RangeNode node)
            {
                this.min = node.min;
                this.max = node.max;
            }

            public override float Evalute(System.Random randomSource, Dictionary<string, object> context)
            {
                var minNum = min.Evalute(randomSource, context);
                var maxNum = max.Evalute(randomSource, context);

                return (float)(randomSource.NextDouble() * (maxNum - minNum) + minNum);
            }
        }
    }
}