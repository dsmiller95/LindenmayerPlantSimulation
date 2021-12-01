using Dman.LSystem.UnityObjects;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Dman.LSystem.Editor.LSystemDebugger
{
    public class LSystemStructureTreeWindow : EditorWindow
    {
        // SerializeField is used to ensure the view state is written to the window 
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] bool showAllSymbols;

        //The TreeView is not serializable, so it should be reconstructed from the tree data.
        LSystemStructureTreeView m_BehaviorTree;
        private void OnEnable()
        {
            // Check whether there is already a serialized view state (state 
            // that survived assembly reloading)
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            m_BehaviorTree = new LSystemStructureTreeView(m_TreeViewState, showAllSymbols);
            UpdateTreeViewTargetSystem();
        }

        private void OnSelectionChange()
        {
            UpdateTreeViewTargetSystem();
        }
        private void OnFocus()
        {
            UpdateTreeViewTargetSystem();
        }

        private void UpdateTreeViewTargetSystem()
        {
            var nextInspectedMachine = GetSelectedMachineIfAny();
            if (nextInspectedMachine == null)
            {
                // the machine is sticky, only switches if you select a different object with a machine attached
                return;
            }
            m_BehaviorTree?.SetInspectedMachine(nextInspectedMachine);
        }

        private LSystemBehavior GetSelectedMachineIfAny()
        {
            var selectedObj = Selection.activeGameObject;
            if (selectedObj == null)
            {
                return null;
            }
            var behaviorMachine = selectedObj.GetComponentInChildren<LSystemBehavior>();
            return behaviorMachine;
        }

        private void OnGUI()
        {
            var nextShowAll = EditorGUILayout.Toggle("Show all symbols", showAllSymbols);
            if(nextShowAll != showAllSymbols)
            {
                showAllSymbols = nextShowAll;
                m_BehaviorTree.SetShowAllSymbols(showAllSymbols);
            }
            m_BehaviorTree.OnGUI(new Rect(0, 20, position.width, position.height));
        }

        [MenuItem("LSystem/Inspector")]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<LSystemStructureTreeWindow>();
            window.titleContent = new GUIContent("Lsystem Tree Inspector");
            window.Show();
        }
    }
}
