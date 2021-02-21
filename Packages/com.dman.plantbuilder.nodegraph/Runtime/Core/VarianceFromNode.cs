using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Variance")] // Add the node in the node creation context menu
    public class VarianceFromNode : BaseNode
    {
        [Input(name = "Origin"), SerializeField]
        public float origin = 1;
        [Input(name = "Delta"), SerializeField]
        public float delta = 2;
        [Output(name = "Out")]
        public float randBetween;

        public override string name => "Variance";

        protected override void Process()
        {
            var rand = (graph as PlantMeshGeneratorGraph).MyRandom;
            randBetween = (float)(origin + (rand.NextDouble() * 2 - 1) * delta);
        }

        //[System.Serializable]
        //class DeferredRangeGenerator : DeferredEvaluator<float>
        //{
        //    private DeferredEvaluator<float> min;
        //    private DeferredEvaluator<float> max;

        //    public DeferredRangeGenerator(RangeNode node)
        //    {
        //        this.min = node.min;
        //        this.max = node.max;
        //    }

        //    public override float Evalute(System.Random randomSource, Dictionary<string, object> context)
        //    {
        //        var minNum = min.Evalute(randomSource, context);
        //        var maxNum = max.Evalute(randomSource, context);

        //        return (float)(randomSource.NextDouble() * (maxNum - minNum) + minNum);
        //    }
        //}
    }
}