using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.Extern
{
    public unsafe partial struct NativeArrayInteropi32
    {
        public NativeArrayInteropi32(NativeArray<int> array)
        {
            data = (int*)array.GetUnsafeReadOnlyPtr();
            len = array.Length;
        }
    }
    public unsafe partial struct NativeArrayInteropf32
    {
        public NativeArrayInteropf32(NativeArray<float> array)
        {
            data = (float*)array.GetUnsafeReadOnlyPtr();
            len = array.Length;
        }
    }
    public unsafe partial struct NativeArrayInteropJaggedIndexing
    {
        public NativeArrayInteropJaggedIndexing(NativeArray<JaggedIndexing> array)
        {
            data = (JaggedIndexing*)array.GetUnsafeReadOnlyPtr();
            len = array.Length;
        }
    }
    public unsafe partial struct NativeArrayInteropLSystemSingleSymbolMatchData
    {
        public NativeArrayInteropLSystemSingleSymbolMatchData(NativeArray<LSystemSingleSymbolMatchData> array)
        {
            data = (LSystemSingleSymbolMatchData*)array.GetUnsafeReadOnlyPtr();
            len = array.Length;
        }
    }
}