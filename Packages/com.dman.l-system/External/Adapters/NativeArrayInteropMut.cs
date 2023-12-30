using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.Extern
{
    public unsafe partial struct NativeArrayInteropi32Mut
    {
        public NativeArrayInteropi32Mut(NativeArray<int> array)
        {
            data = (int*)array.GetUnsafePtr();
            len = array.Length;
        }
    }
    public unsafe partial struct NativeArrayInteropf32Mut
    {
        public NativeArrayInteropf32Mut(NativeArray<float> array)
        {
            data = (float*)array.GetUnsafePtr();
            len = array.Length;
        }
    }
    public unsafe partial struct NativeArrayInteropJaggedIndexingMut
    {
        public NativeArrayInteropJaggedIndexingMut(NativeArray<JaggedIndexing> array)
        {
            data = (JaggedIndexing*)array.GetUnsafePtr();
            len = array.Length;
        }
    }
}