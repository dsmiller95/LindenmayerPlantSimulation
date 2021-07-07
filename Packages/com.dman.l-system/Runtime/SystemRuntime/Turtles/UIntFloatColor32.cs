using System.Runtime.InteropServices;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UIntFloatColor32
    {
        [FieldOffset(0)]
        public uint UIntValue;
        [FieldOffset(0)]
        public float FloatValue;
        [FieldOffset(0)]
        public Color32 color;

        [FieldOffset(0)]
        public byte colorR;
        [FieldOffset(1)]
        public byte colorG;
        [FieldOffset(2)]
        public byte colorB;
        [FieldOffset(3)]
        public byte colorA;
        public UIntFloatColor32(Color32 value)
        {
            this.UIntValue = default;
            this.FloatValue = default;
            colorR = default;
            colorG = default;
            colorB = default;
            colorA = default;

            this.color = value;

        }
        public UIntFloatColor32(float value)
        {
            this.UIntValue = default;
            this.color = default;
            colorR = default;
            colorG = default;
            colorB = default;
            colorA = default;

            this.FloatValue = value;
        }
        public UIntFloatColor32(uint value)
        {
            this.FloatValue = default;
            this.color = default;
            colorR = default;
            colorG = default;
            colorB = default;
            colorA = default;

            this.UIntValue = value;
        }
    }

}
