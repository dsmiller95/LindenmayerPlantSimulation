using Dman.LSystem.Extern;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    public class NativeArrayBuilder<T> where T : struct
    {
        public NativeArray<T> data;
        public int filledIndexInData;
        public NativeArrayBuilder(int totalMemSpace, Allocator allocator = Allocator.Persistent)
        {
            data = new NativeArray<T>(totalMemSpace, allocator);
            filledIndexInData = 0;
        }

        public JaggedIndexing WriteDataFromArray(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                data[filledIndexInData + i] = array[i];
            }
            var jaggedIndex = new JaggedIndexing
            {
                index = filledIndexInData,
                length = (ushort)array.Length
            };
            filledIndexInData += array.Length;
            return jaggedIndex;
        }
    }
}
