using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GraphProcessor;
using System;
using UnityEditor;
using System.Collections.Generic;

namespace PlantBuilder.NodeGraph
{
    public class PlantMeshGeneratorView: BaseGraphView
	{
        public static Dictionary<string, object> DEFAULT_CONTEXT = new Dictionary<string, object>();
        public static string DEFAULT_MATERIAL_NAME = "defaultMaterial";

        public event Action onWindowDisposed;

		public PlantMeshGeneratorView(EditorWindow window) : base(window) { }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();

            foreach (var nodeMenuItem in NodeProvider.GetNodeMenuEntries())
            {
                var mousePos = (evt.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
                Vector2 nodePosition = mousePos;
                evt.menu.AppendAction("Create/" + nodeMenuItem.path,
                    (e) => CreateNodeOfType(nodeMenuItem.type, nodePosition),
                    DropdownMenuAction.AlwaysEnabled
                );
            }

            base.BuildContextualMenu(evt);
        }

        void CreateNodeOfType(Type type, Vector2 position)
		{
			RegisterCompleteObjectUndo("Added " + type + " node");
			AddNode(BaseNode.CreateFromType(type, position));
		}

        private bool isDisposed = false;
        public new void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            onWindowDisposed?.Invoke();
            base.Dispose();
        }
    }
}
