using GraphProcessor;
using System;
using System.Linq;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Mesh
{
    [System.Serializable, NodeMenuItem("Mesh/Output")] // Add the node in the node creation context menu
    public class MeshGraphOutputNode : BaseNode
    {
        [Input(name = "Draft")]
        public DeferredMeshEvaluator draft;

        public override string name => "Output";

        protected override void Process()
        {
            var serializedOutput = SerializedDeferredMeshEvaluator.GetFromInstance(draft);
            var outputParam = graph.GetExposedParameter("output");
            Debug.Log(outputParam.guid);
            graph.UpdateExposedParameter(outputParam.guid, serializedOutput);
            Debug.Log(serializedOutput);
            Debug.Log(serializedOutput.GetStringRepresentation());
        }
    }
}