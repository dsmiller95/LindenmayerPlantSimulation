using GraphProcessor;
using SplineMesh;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("BezierCurve")]
    public class BezierCurveNode : BaseNode
    {
        [Input(name = "NodeA")]
        public SplineNode nodeA;
        [Input(name = "NodeB")]
        public SplineNode nodeB;

        [Output(name = "Out")]
        public CubicBezierCurve output;

        public override string name => "BezierCurve";

        protected override void Process()
        {
            output = new CubicBezierCurve(
                    nodeA,
                    nodeB);
        }

        //[System.Serializable]
        //class CubicBezierCurveCombinator : DeferredEvaluator<CubicBezierCurve>
        //{
        //    DeferredEvaluator<SplineNode> nodeA;
        //    DeferredEvaluator<SplineNode> nodeB;

        //    public CubicBezierCurveCombinator(BezierCurveNode node)
        //    {
        //        nodeA = node.nodeA;
        //        nodeB = node.nodeB;
        //    }

        //    public override CubicBezierCurve Evalute(System.Random randomSource, Dictionary<string, object> context)
        //    {
        //        return new CubicBezierCurve(
        //            nodeA.Evalute(randomSource, context),
        //            nodeB.Evalute(randomSource, context));
        //    }
        //}
    }
}