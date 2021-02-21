using GraphProcessor;

namespace PlantBuilder.NodeGraph
{
    public class PlantMeshGeneratorToolbarView : ToolbarView
    {
        BaseGraphProcessor processor;

        public PlantMeshGeneratorToolbarView(PlantMeshGeneratorView graphView, PlantMeshGeneratorGraph baseGraph) : base(graphView)
        {
            processor = new ProcessGraphProcessor(baseGraph);
            graphView.computeOrderUpdated += processor.UpdateComputeOrder;
        }

        protected override void AddButtons()
        {
            base.AddButtons();
            var graph = graphView.graph as PlantMeshGeneratorGraph;
            AddButton(
                "reseed",
                () =>
                {
                    graph.Reseed();
                    processor.Run();

                    //graph.pinnedElements.TryGetValue(typeof(ProcessorView), out var view);
                });
        }
    }
}
