using GraphProcessor;
using UnityEditor;
using UnityEngine;

namespace PlantBuilder.NodeGraph
{
    public class PlantMeshGeneratorWindow: BaseGraphWindow
	{
        protected override void OnDisable()
		{
			base.OnDisable();
			(graphView as PlantMeshGeneratorView)?.Dispose();
        }

        protected override void InitializeWindow(BaseGraph graph)
		{
			titleContent = new GUIContent("Plant builder");

			if (graphView == null)
			{
				graphView = new PlantMeshGeneratorView(this);
				var baseGraph = graph as PlantMeshGeneratorGraph;
				var toolbarView = new PlantMeshGeneratorToolbarView(graphView as PlantMeshGeneratorView, baseGraph);
				graphView.Add(toolbarView) ;
			}

			// TODO: left this out since it creates two mini map views for some reason
			//graphView.Add(new MiniMapView(graphView));

			rootView.Add(graphView);
		}
	}
}
