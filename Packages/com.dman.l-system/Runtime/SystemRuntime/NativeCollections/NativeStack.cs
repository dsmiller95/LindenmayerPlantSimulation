using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    public struct TmpNativeStack<T> : INativeDisposable where T : unmanaged
    {
        private NativeList<T> backingList;
        private int indexInStack;

        public TmpNativeStack(int initialCapacity, Allocator allocator = Allocator.Temp)
        {
            backingList = new NativeList<T>(initialCapacity, allocator);
            indexInStack = 0;
        }

        public int Count => indexInStack;

        public void Push(T item)
        {
            if (backingList.Capacity < indexInStack + 1)
            {
                backingList.Resize(backingList.Capacity + 5, NativeArrayOptions.UninitializedMemory);
            }
            if (backingList.Length == indexInStack)
            {
                backingList.Add(item);
            }
            else
            {
                backingList[indexInStack] = item;
            }
            indexInStack++;
        }

        public T Pop()
        {
            var lastVal = backingList[indexInStack - 1];
            indexInStack--;
            return lastVal;
        }
        public void Reset()
        {
            indexInStack = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return backingList.Dispose(inputDeps);
        }

        public void Dispose()
        {
            backingList.Dispose();
        }
    }
}
