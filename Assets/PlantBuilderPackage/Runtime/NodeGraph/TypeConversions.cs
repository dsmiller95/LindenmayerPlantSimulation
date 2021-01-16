﻿
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

        //public static SerializedDeferredMeshEvaluator ConvertSerializeDeferredMeshEvaluator(DeferredMeshEvaluator from) => SerializedDeferredMeshEvaluator.GetFromInstance(from);
        //public static DeferredMeshEvaluator ConvertDeserializeDeferredMeshEvaluator(SerializedDeferredMeshEvaluator from) => from.GetSerializedGuy();

        public override IEnumerable<(Type, Type)> GetIncompatibleTypes()
        {
            yield return (typeof(DeferredMeshEvaluator), typeof(object));
            yield return (typeof(RelayNode.PackedRelayData), typeof(object));
        }
    }
}
