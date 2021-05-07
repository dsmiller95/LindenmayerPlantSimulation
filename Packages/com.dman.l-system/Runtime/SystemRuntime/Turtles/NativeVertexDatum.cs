using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct NativeVertexDatum
    {
        public float3 vertex;
        public float3 normal;
        public float2 uv;
        public float4 tangent;
    }
}
