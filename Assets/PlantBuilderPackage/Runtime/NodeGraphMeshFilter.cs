using PlantBuilder.NodeGraph;
using UnityEngine;

namespace PlantBuilder
{
    [RequireComponent(typeof(MeshFilter))]
    public class NodeGraphMeshFilter : MonoBehaviour
    {
        public PlantMeshGeneratorGraph generatorGraph;

        private void Start()
        {
            var generatedPlant = generatorGraph.GenerateMesh(true);
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = generatedPlant.meshDraft.ToMesh(true, true);
        }
    }
}
