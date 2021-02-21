using Dman.MeshDraftExtensions;
using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using PlantBuilder.NodeGraph.MeshNodes;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph
{
    [FieldDrawer(typeof(MeshDraftWithExtras))]
    public class MeshDraftWithExtrasDrawer : VisualElement, INotifyValueChanged<MeshDraftWithExtras>
    {
        private Label label;

        public MeshDraftWithExtrasDrawer()
        {
            label = new Label("poop");
            this.Add(label);
        }

        public MeshDraftWithExtras value { get => default; set => SetValueWithoutNotify(value); }

        public void SetValueWithoutNotify(MeshDraftWithExtras newValue)
        {
            var newText = newValue.bounds.size.ToString();//.GetStringRepresentation();
            label.text = newText;
        }
    }
}
