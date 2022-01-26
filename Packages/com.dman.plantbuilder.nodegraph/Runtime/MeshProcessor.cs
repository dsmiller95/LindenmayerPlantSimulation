using GraphProcessor;

namespace PlantBuilder.NodeGraph
{
    public class MeshProcessor : ProcessGraphProcessor
    {
        private PlantMeshGeneratorGraph meshGraph => graph as PlantMeshGeneratorGraph;

        public MeshProcessor(PlantMeshGeneratorGraph graph) : base(graph) { }

        public override void Run()
        {
            meshGraph.ResetRandom();
            base.Run();
        }
    }
}
