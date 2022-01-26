
using GraphProcessor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder.NodeGraph
{

    public class CustomConvertions : ITypeAdapter
    {
        public static Vector4 ConvertFloatToVector4(float from)
        {
            return new Vector4(from, from, from, from);
        }

        public static float ConvertVector4ToFloat(Vector4 from)
        {
            return from.x;
        }
        public override IEnumerable<(Type, Type)> GetIncompatibleTypes()
        {
            //yield return (typeof(DeferredEvaluator<MeshDraftWithExtras>), typeof(object));
            //yield return (typeof(DeferredEvaluator<float>), typeof(object));
            yield return (typeof(float), typeof(object));
            //yield return (typeof(DeferredEvaluator<MeshDraftWithExtras>), typeof(RelayNode.PackedRelayData));
            //yield return (typeof(DeferredEvaluator<float>), typeof(RelayNode.PackedRelayData));
            yield return (typeof(float), typeof(RelayNode.PackedRelayData));
            yield return (typeof(RelayNode.PackedRelayData), typeof(object));
        }
    }
}
