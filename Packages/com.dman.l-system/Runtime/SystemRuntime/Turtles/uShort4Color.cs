using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct uShort4Color
    {
        public UInt16 r;
        public UInt16 g;
        public UInt16 b;
        public UInt16 a;

        /// <summary>
        /// Assumes sourceFloats vary from 0 to 1
        /// </summary>
        /// <param name="sourceFloats"></param>
        public uShort4Color(float4 sourceFloats)
        {
            this.r = (ushort)(sourceFloats.x * ushort.MaxValue);
            this.g = (ushort)(sourceFloats.y * ushort.MaxValue);
            this.b = (ushort)(sourceFloats.z * ushort.MaxValue);
            this.a = (ushort)(sourceFloats.w * ushort.MaxValue);
        }
    }
}
