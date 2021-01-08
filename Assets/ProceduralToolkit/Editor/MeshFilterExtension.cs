using UnityEngine;
using UnityEditor;

namespace ProceduralToolkit.Editor
{
    /// <summary>
    /// Mesh saving utility available at `MeshFilter context menu > Save Mesh`
    /// </summary>
    public class MeshFilterExtension
    {
        private const string menuPath = "CONTEXT/MeshFilter/Save Mesh";

        [MenuItem(menuPath)]
        private static void SaveMesh(MenuCommand menuCommand)
        {
            var meshFilter = (MeshFilter) menuCommand.context;
            var mesh = meshFilter.sharedMesh;

            var path = EditorUtility.SaveFilePanelInProject("Save Mesh", mesh.name, "asset", "Save Mesh");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            AssetDatabase.CreateAsset(mesh, path);
        }

        [MenuItem(menuPath, true)]
        private static bool SaveMeshTest(MenuCommand menuCommand)
        {
            var meshFilter = (MeshFilter) menuCommand.context;
            return meshFilter.sharedMesh != null;
        }
    }
}
