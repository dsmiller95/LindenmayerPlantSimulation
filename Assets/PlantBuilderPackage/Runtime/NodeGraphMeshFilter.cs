using PlantBuilder.NodeGraph;
using PlantBuilder.NodeGraph.Mesh;
using System.Linq;
using UnityEngine;

namespace PlantBuilder
{
    [RequireComponent(typeof(MeshFilter))]
    public class NodeGraphMeshFilter : MonoBehaviour
    {
        public PlantMeshGeneratorGraph generatorGraph;

        private void Start()
        {
            var meshGeneratorOutput = generatorGraph.GetExposedParameter("output");
            meshGeneratorOutput.serializedValue.OnAfterDeserialize();
            var serializedGenerator = meshGeneratorOutput.serializedValue.value as SerializedDeferredMeshEvaluator;
            if(serializedGenerator == null)
            {
                Debug.LogError("'output mesh' parameter not defined");
                return;
            }

            var generator = serializedGenerator.GetDeserializedGuy();

            //var outputNode = generatorGraph.graphOutputs.Where(x => x is MeshGraphOutputNode).Select(x => x as MeshGraphOutputNode).FirstOrDefault();
            //if (outputNode == null)
            //{
            //    return;
            //}
            //var meshGenerator = outputNode.draft;
            var meshFilter = GetComponent<MeshFilter>();
            var meshDraft = generator.Evalute(null);
            meshFilter.mesh = meshDraft.meshDraft.ToMesh(true, true);
        }
    }
}
