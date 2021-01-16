using GraphProcessor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph.Core
{
    [NodeCustomEditor(typeof(NumberNode))]
    public class NumberNodeView : BaseNodeView
    {
        public override void Enable()
        {
            var floatNode = nodeTarget as NumberNode;

            DoubleField floatField = new DoubleField
            {
                value = floatNode.input
            };

            floatNode.onProcessed += () => floatField.value = floatNode.input;

            floatField.RegisterValueChangedCallback((v) =>
            {
                owner.RegisterCompleteObjectUndo("Updated floatNode input");
                floatNode.input = (float)v.newValue;
            });

            controlsContainer.Add(floatField);
        }
    }
}