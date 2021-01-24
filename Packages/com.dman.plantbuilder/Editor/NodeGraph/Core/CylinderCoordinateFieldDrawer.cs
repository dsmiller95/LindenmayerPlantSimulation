using GraphProcessor;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph.Core.Vectors
{
    [FieldDrawer(typeof(CylinderCoordinate))]
    public class CylinderCoordinateFieldDrawer : Vector3Field, INotifyValueChanged<CylinderCoordinate>
    {
        public CylinderCoordinateFieldDrawer(): base()
        {
        }
        public CylinderCoordinateFieldDrawer(string label): base(label)
        {
        }

        public override Vector3 value { get => base.value; set
            {
                var trueValue = new CylinderCoordinate
                {
                    y = value.y,
                    axialDistance = value.x,
                    azimuth = value.z
                };
                (this as INotifyValueChanged<CylinderCoordinate>).value = trueValue;
            }
        }

        CylinderCoordinate INotifyValueChanged<CylinderCoordinate>.value
        {
            get
            {
                var val = (this as Vector3Field).value;
                return new CylinderCoordinate
                {
                    y = val.y,
                    axialDistance = val.x,
                    azimuth = val.z
                };
            }
            set
            {
                var lastVal = (this as INotifyValueChanged<CylinderCoordinate>).value;
                if (!lastVal.Equals(value))
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<CylinderCoordinate> evt = ChangeEvent<CylinderCoordinate>.GetPooled(lastVal, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

        public void SetValueWithoutNotify(CylinderCoordinate newValue)
        {
            this.SetValueWithoutNotify(new Vector3
            {
                y = newValue.y,
                x = newValue.axialDistance,
                z = newValue.azimuth
            });
        }
    }
}
