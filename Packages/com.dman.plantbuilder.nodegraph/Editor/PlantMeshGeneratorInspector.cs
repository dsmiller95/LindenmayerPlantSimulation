using GraphProcessor;
using UnityEditor;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph
{
    [CustomEditor(typeof(PlantMeshGeneratorGraph), true)]
    public class PlantMeshGeneratorInspector : GraphInspector
    {
        protected override void CreateInspector()
        {
            base.CreateInspector();

            root.Add(new Button(() => EditorWindow.GetWindow<PlantMeshGeneratorWindow>().InitializeGraph(target as BaseGraph))
            {
                text = "Open base graph window"
            });
        }
    }
}