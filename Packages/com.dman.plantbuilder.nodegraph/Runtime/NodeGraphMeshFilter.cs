using GraphProcessor;
using PlantBuilder.NodeGraph;
using UnityEngine;

namespace PlantBuilder
{
    [RequireComponent(typeof(MeshFilter))]
    public class NodeGraphMeshFilter : MonoBehaviour
    {
        public PlantMeshGeneratorGraph generatorGraph;
        private ProcessGraphProcessor processor;

        private void Start()
        {
            if (generatorGraph != null)
                processor = new ProcessGraphProcessor(generatorGraph);
            this.GenerateMesh();
        }

        public void GenerateMesh()
        {
            if (processor == null)
            {
                return;
            }

            processor.Run();

            //var output = generatorGraph.GetParameterValue("output") as SerializedDeferredMeshEvaluator;

            var generatedPlant = generatorGraph.GenerateMesh(true);
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = generatedPlant.ToMesh();
        }
    }
}
