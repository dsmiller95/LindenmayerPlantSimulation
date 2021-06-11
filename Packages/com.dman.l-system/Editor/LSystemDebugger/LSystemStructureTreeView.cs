using Dman.LSystem.UnityObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace Dman.LSystem.Editor.LSystemDebugger
{
    public class LSystemStructureTreeView : TreeView
    {
        public LSystemStructureTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            Reload();
        }

        private LSystemBehavior inspectedMachine;
        public void SetInspectedMachine(LSystemBehavior newMachine)
        {
            if (newMachine != inspectedMachine)
            {
                if(inspectedMachine != null)
                {
                    inspectedMachine.OnSystemStateUpdated -= this.LSystemStateWasUpdated;
                }
                inspectedMachine = newMachine;
                inspectedMachine.OnSystemStateUpdated += this.LSystemStateWasUpdated;
            }
        }

        private void LSystemStateWasUpdated()
        {
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            if (inspectedMachine == null)
            {
                return EmptyTree("Nothing selected ya dingus");
            }

            var stepper = inspectedMachine.steppingHandle.Stepper();
            try
            {
                var rootNode = LSystemStructureTreeElement.ConstructTreeFromString(
                    inspectedMachine.steppingHandle.currentState.currentSymbols.Data,
                    stepper.includedCharacters.Aggregate(new HashSet<int>(), (acc, next) =>
                    {
                        acc.UnionWith(next);
                        return acc;
                    }),
                    stepper.branchOpenSymbol,
                    stepper.branchCloseSymbol
                    );
                rootNode.depth = -1;

                // Utility method that initializes the TreeViewItem.children and .parent for all items.
                //SetupParentsAndChildrenFromDepths(root, allItems);
                SetupDepthsFromParentsAndChildren(rootNode);

                // Return root of the tree
                return rootNode;
            }catch (Exception e)
            {
                Debug.LogException(e);
                return EmptyTree("Exception encountered when building tree");
            }
        }

        private TreeViewItem EmptyTree(string message)
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            root.AddChild(new TreeViewItem { id = 1, depth = 0, displayName = message });
            return root;
        }

        private static readonly float ExecutionTimeFadeoutSeconds = 3;

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is LSystemStructureTreeElement behaviorNode)
            {
                var backgroundColoring = behaviorNode.GetBackgroundColoring(ExecutionTimeFadeoutSeconds);
                var rect = args.rowRect;
                EditorGUI.DrawRect(rect, backgroundColoring);
            }
            base.RowGUI(args);
        }
    }
}
