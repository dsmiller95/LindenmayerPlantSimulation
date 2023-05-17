using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.Extern
{
    public unsafe struct NativeArrayInterop<T> where T : unmanaged
    {
        public unsafe T* data;
        public int len;

        public NativeArrayInterop(NativeArray<T> array)
        {
            data = (T*)array.GetUnsafePtr();
            len = array.Length;
        }
        
        public static implicit operator NativeArrayInterop<T>(NativeArray<T> b) => new NativeArrayInterop<T>(b);
    }

    public unsafe partial struct NativeArrayInteropi32
    {
        public static implicit operator NativeArrayInteropi32(NativeArrayInterop<int> b) => new NativeArrayInteropi32()
        {
            data = b.data,
            len = b.len
        };
        public static implicit operator NativeArrayInterop<int>(NativeArrayInteropi32 b) => new NativeArrayInterop<int>()
        {
            data = b.data,
            len = b.len
        };
    }
    public unsafe partial struct NativeArrayInteropf32
    {
        public static implicit operator NativeArrayInteropf32(NativeArrayInterop<float> b) => new NativeArrayInteropf32()
        {
            data = b.data,
            len = b.len
        };
        public static implicit operator NativeArrayInterop<float>(NativeArrayInteropf32 b) => new NativeArrayInterop<float>()
        {
            data = b.data,
            len = b.len
        };
    }
    public unsafe partial struct NativeArrayInteropJaggedIndexing
    {
        public static implicit operator NativeArrayInteropJaggedIndexing(NativeArrayInterop<JaggedIndexing> b) => new NativeArrayInteropJaggedIndexing()
        {
            data = b.data,
            len = b.len
        };
        public static implicit operator NativeArrayInterop<JaggedIndexing>(NativeArrayInteropJaggedIndexing b) => new NativeArrayInterop<JaggedIndexing>()
        {
            data = b.data,
            len = b.len
        };
    }
    
    
}