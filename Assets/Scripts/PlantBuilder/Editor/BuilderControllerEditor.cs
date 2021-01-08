using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlantBuilder
{
    [CustomEditor(typeof(BuilderController))]
    public class BuilderControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Reseed"))
            {
                var self = serializedObject.targetObject as BuilderController;
                self.ResetSeed();
                EditorUtility.SetDirty(self);
            }
        }
    }
}