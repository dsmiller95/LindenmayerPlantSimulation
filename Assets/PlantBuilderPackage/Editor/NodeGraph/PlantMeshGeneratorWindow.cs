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
			base.OnDestroy();
		}
        protected override void OnDisable()
		{
			graphView?.Dispose();
			base.OnDisable();
        }
        //private void OnEnable()
        //{
            
        //}

        protected override void InitializeWindow(BaseGraph graph)
		{
			titleContent = new GUIContent("Plant builder");

			if (graphView == null)
				graphView = new PlantMeshGeneratorView(this);

			// TODO: left this out since it creates two mini map views for some reason
			//graphView.Add(new MiniMapView(graphView));
			var baseGraph = graph as PlantMeshGeneratorGraph;
			var toolbarView = new PlantMeshGeneratorToolbarView(graphView as PlantMeshGeneratorView, baseGraph);
			graphView.Add(toolbarView);

			rootView.Add(graphView);
		}
	}
}
