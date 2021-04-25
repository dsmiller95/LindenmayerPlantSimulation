using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    public struct TmpNativeStack<T> where T:unmanaged
    {
        private NativeList<T> backingList;
        private int indexInStack;

        public TmpNativeStack(int initialCapacity)
        {
            backingList = new NativeList<T>(initialCapacity, Allocator.Temp);
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
    }
}
