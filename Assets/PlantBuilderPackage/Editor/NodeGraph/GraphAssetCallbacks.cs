using PlantBuilder.NodeGraph.DeferredEvaluators;
using PlantBuilder.NodeGraph.MeshNodes;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace PlantBuilder.NodeGraph
{
    public class GraphAssetCallbacks
    {
        [MenuItem("Assets/Create/MeshGenerator", false, 10)]
        public static void CreateGraphProcessor()
        {
            var graph = ScriptableObject.CreateInstance<PlantMeshGeneratorGraph>();

            var outputNode = new MeshGraphOutputNode();
            outputNode.position = new Rect(
                new Vector2(200, 200),
                new Vector2(100, 100));
            outputNode.OnNodeCreated();
            graph.AddNode(outputNode);

            graph.AddExposedParameter(
                "output",
                typeof(SerializedDeferredMeshEvaluator),
                null);
            ProjectWindowUtil.CreateAsset(graph, "MeshGenerator.asset");
        }

        [OnOpenAsset(0)]
        public static bool OnBaseGraphOpened(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as PlantMeshGeneratorGraph;

            if (asset != null)
            {
                EditorWindow.GetWindow<PlantMeshGeneratorWindow>().InitializeGraph(asset);
                return true;
            }
            return false;
        }
    }
}
