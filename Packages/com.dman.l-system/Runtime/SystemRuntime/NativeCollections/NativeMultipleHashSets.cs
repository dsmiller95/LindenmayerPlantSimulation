using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{

    public struct NativeMultipleHashSets :
        IDisposable,
        INativeDisposable
    {
        [NativeDisableParallelForRestriction]
        private NativeHashSet<HashSetKey> data;
        public bool IsCreated => data.IsCreated;

        public HashSetSlice this[short index] => new HashSetSlice(this, index);

        public NativeMultipleHashSets(int initialCapacity, Allocator allocator)
        {
            data = new NativeHashSet<HashSetKey>(initialCapacity, allocator);
        }
        public NativeMultipleHashSets(ISet<int>[] initialValues, Allocator allocator)
        {
            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            data = new NativeHashSet<HashSetKey>(initialValues.Sum(x => x.Count), allocator);
            UnityEngine.Profiling.Profiler.EndSample();
            if (initialValues.Length >= short.MaxValue)
            {
                throw new Exception("Too many individual sets to be stored in a short");
            }
            for (short i = 0; i < initialValues.Length; i++)
            {
                var set = initialValues[i];
                if (set == null || set.Count <= 0)
                {
                    continue;
                }
                var hashValue = new HashSetKey
                {
                    index = i
                };
                foreach (var value in set)
                {
                    hashValue.data = value;
                    data.Add(hashValue);
                }
            }
        }

        struct HashSetKey : IEquatable<HashSetKey>
        {
            public int data;
            public short index;

            public HashSetKey(short index, int data)
            {
                this.data = data;
                this.index = index;
            }

            public bool Equals(HashSetKey other)
            {
                return other.index == index && other.data.Equals(data);
            }

            public override int GetHashCode()
            {
                return data.GetHashCode() ^ index.GetHashCode();
            }
        }

        public struct HashSetSlice
        {
            short sliceIndex;
            NativeMultipleHashSets sourceData;
            public HashSetSlice(NativeMultipleHashSets sourceData, short sliceIndex)
            {
                this.sliceIndex = sliceIndex;
                this.sourceData = sourceData;
            }

            public bool Contains(int item)
            {
                return sourceData.Contains(sliceIndex, item);
            }
            public bool Add(int item)
            {
                return sourceData.Add(sliceIndex, item);
            }
            public bool Remove(int item)
            {
                return sourceData.Remove(sliceIndex, item);
            }
        }

        public bool Contains(short index, int item)
        {
            var dataItem = new HashSetKey(index, item);
            return data.Contains(dataItem);
        }

        public bool Add(short index, int item)
        {
            var dataItem = new HashSetKey(index, item);
            return data.Add(dataItem);
        }

        public bool Remove(short index, int item)
        {
            var dataItem = new HashSetKey(index, item);
            return data.Remove(dataItem);
        }

        public void Dispose()
        {
            data.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return data.Dispose(inputDeps);
        }
    }
}