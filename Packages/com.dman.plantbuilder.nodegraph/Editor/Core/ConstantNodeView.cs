using GraphProcessor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph.Core
{
    [NodeCustomEditor(typeof(ConstantNode))]
    public class ConstantNodeView : BaseNodeView
    {
        enum ConstantSelection
        {
            PI,
            TWO_PI
        }

        static float GetConstant(ConstantSelection select)
        {
            switch (select)
            {
                case ConstantSelection.PI:
                    return Mathf.PI;
                case ConstantSelection.TWO_PI:
                    return Mathf.PI * 2;
                default:
                    return 0;
            }
        }

        public override void Enable()
        {
            var target = nodeTarget as ConstantNode;

            var constantChoiceField = new EnumField(ConstantSelection.PI);

            constantChoiceField.RegisterValueChangedCallback((v) =>
            {
                owner.RegisterCompleteObjectUndo("Updated floatNode input");
                var enumVal = (ConstantSelection)v.newValue;
                target.output = GetConstant(enumVal);
            });

            controlsContainer.Add(constantChoiceField);
        }
    }
}