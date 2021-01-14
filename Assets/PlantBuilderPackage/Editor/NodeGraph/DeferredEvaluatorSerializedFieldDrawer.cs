using GraphProcessor;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph
{
    [FieldDrawer(typeof(SerializedDeferredMeshEvaluator))]
    public class DeferredEvaluatorSerializedFieldDrawer : VisualElement, INotifyValueChanged<SerializedDeferredMeshEvaluator>
    {
        private Label label;

        public DeferredEvaluatorSerializedFieldDrawer()
        {
            label = new Label("poop");
            this.Add(label);
        }

        public SerializedDeferredMeshEvaluator value { get => null; set => SetValueWithoutNotify(value); }

        public void SetValueWithoutNotify(SerializedDeferredMeshEvaluator newValue)
        {
            var newText = newValue.GetStringRepresentation();
            label.text = newText;
        }
    }
}
