using GraphProcessor;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("Numbers/Range")] // Add the node in the node creation context menu
    public class RangeNode : BaseNode
    {
        [Input(name = "Min"), SerializeField]
        public float min = 1;
        [Input(name = "Max"), SerializeField]
        public float max = 2;
        [Output(name = "Out")]
        public float output;

        public override string name => "Range";

        protected override void Process()
        {
            var trueGraph = graph as PlantMeshGeneratorGraph;
            output = (float)(trueGraph.MyRandom.NextDouble() * (max - min) + min);
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