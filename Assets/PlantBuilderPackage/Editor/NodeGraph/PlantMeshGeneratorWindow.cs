using GraphProcessor;
using UnityEditor;
using UnityEngine;

namespace PlantBuilder.NodeGraph
{
    public class PlantMeshGeneratorWindow: BaseGraphWindow
	{
		protected override void OnDestroy()
		{
			graphView?.Dispose();
		}

		protected override void InitializeWindow(BaseGraph graph)
		{
			titleContent = new GUIContent("Plant builder");

			if (graphView == null)
				graphView = new PlantMeshGeneratorView(this);

			// TODO: left this out since it creates two mini map views for some reason
			//graphView.Add(new MiniMapView(graphView));
			var toolbarView = new ToolbarView(graphView);
			graphView.Add(toolbarView);

			rootView.Add(graphView);
		}
	}
}
