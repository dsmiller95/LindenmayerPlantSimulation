using System;
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

        public bool CanPop()
        {
            return indexInStack >= 1;
        }

        public T Pop()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!CanPop())
            {
                throw new InvalidOperationException("Attempted to pop from an empty stack");
            }
#endif
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
