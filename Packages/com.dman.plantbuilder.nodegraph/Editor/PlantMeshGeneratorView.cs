using GraphProcessor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph
{
    public class PlantMeshGeneratorView : BaseGraphView
    {
        public static Dictionary<string, object> DEFAULT_CONTEXT = new Dictionary<string, object>();
        public static string DEFAULT_MATERIAL_NAME = "defaultMaterial";

        public event Action onWindowDisposed;

        public PlantMeshGeneratorView(EditorWindow window) : base(window)
        {

        }

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

        //[return: TupleElementNames(new[] { "path", "type" })]
        public override IEnumerable<(string path, Type type)> FilterCreateNodeMenuEntries()
        {
            var options = base.FilterCreateNodeMenuEntries();

            return options.Where(x => !x.path.ToLower().Contains("relay"));
        }
    }
}
