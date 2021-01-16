using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using System;
using System.Linq;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Output")] // Add the node in the node creation context menu
    public class MeshGraphOutputNode : BaseNode
    {
        [Input(name = "Draft")]
        public DeferredEvaluator<PlantMeshComponent> draft;

        public override string name => "Output";

        protected override void Process()
        {

            var serializedOutput = draft == null ? null : SerializedDeferredMeshEvaluator.GetFromInstance(draft);
            var outputParam = graph.GetExposedParameter("output");
            graph.UpdateExposedParameter(outputParam.guid, serializedOutput);

            var typedGraph = graph as PlantMeshGeneratorGraph;
            typedGraph.ResetRandom();

            
        }
    }
}