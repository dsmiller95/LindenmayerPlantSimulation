using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using SplineMesh;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [System.Serializable, NodeMenuItem("BezierCurve")]
    public class BezierCurveNode : BaseNode
    {
        [Input(name = "NodeA")]
        public DeferredEvaluator<SplineNode> nodeA;
        [Input(name = "NodeB")]
        public DeferredEvaluator<SplineNode> nodeB;

        [Output(name = "Out")]
        public DeferredEvaluator<CubicBezierCurve> output;

        public override string name => "BezierCurve";

        protected override void Process()
        {
            output = new CubicBezierCurveCombinator(this);
        }

        [System.Serializable]
        class CubicBezierCurveCombinator : DeferredEvaluator<CubicBezierCurve>
        {
            DeferredEvaluator<SplineNode> nodeA;
            DeferredEvaluator<SplineNode> nodeB;

            public CubicBezierCurveCombinator(BezierCurveNode node)
            {
                nodeA = node.nodeA;
                nodeB = node.nodeB;
            }

            public override CubicBezierCurve Evalute(System.Random randomSource, Dictionary<string, object> context)
            {
                return new CubicBezierCurve(
                    nodeA.Evalute(randomSource, context),
                    nodeB.Evalute(randomSource, context));
            }
        }
    }
}