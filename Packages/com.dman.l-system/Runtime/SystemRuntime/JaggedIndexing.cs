using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct JaggedNativeArray<T> : System.IEquatable<JaggedNativeArray<T>>, IDisposable  where T: unmanaged
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<T> data;
        [NativeDisableParallelForRestriction]
        public NativeArray<JaggedIndexing> indexing;

        public JaggedNativeArray(JaggedNativeArray<T> jaggedData, Allocator allocator)
        {
            indexing = new NativeArray<JaggedIndexing>(jaggedData.indexing, allocator);
            data = new NativeArray<T>(jaggedData.data, allocator);
        }

        public JaggedNativeArray(int firstDimensionSize, int totalDataSize, Allocator allocator, NativeArrayOptions initializationOptions = NativeArrayOptions.UninitializedMemory)
        {
            indexing = new NativeArray<JaggedIndexing>(firstDimensionSize, allocator, initializationOptions);
            data = new NativeArray<T>(totalDataSize, allocator, initializationOptions);
        }

        public JaggedNativeArray(T[][] jaggedData, Allocator allocator)
        {
            indexing = new NativeArray<JaggedIndexing>(jaggedData.Length, allocator, NativeArrayOptions.UninitializedMemory);

            var paramSum = jaggedData.Select(x => x.Length).Sum();
            data = new NativeArray<T>(paramSum, allocator, NativeArrayOptions.UninitializedMemory);

            paramSum = 0;
            for (int i = 0; i < jaggedData.Length; i++)
            {
                indexing[i] = new JaggedIndexing
                {
                    index = (int)paramSum,
                    length = (ushort)jaggedData[i].Length
                };
                for (int j = 0; j < jaggedData[i].Length; j++)
                {
                    data[paramSum + j] = jaggedData[i][j];
                }
                paramSum += jaggedData[i].Length;
            }
        }

        public int Length => indexing.Length;

        public bool IsCreated => data.IsCreated && indexing.IsCreated;
        public JaggedIndexing this[int index]
        {
            get
            {
                return indexing[index];
            }
            set
            {
                indexing[index] = value;
            }
        }
        public T this[int index, int indexInJagged]
        {
            get
            {
                var jagged = indexing[index];
                var realIndex = jagged.index + indexInJagged;
                return data[realIndex];
            }
            set
            {
                var jagged = indexing[index];
                var realIndex = jagged.index + indexInJagged;
                data[realIndex] = value;
            }
        }
        public T this[JaggedIndexing jagged, int indexInJagged]
        {
            get
            {
                var realIndex = jagged.index + indexInJagged;
                return data[realIndex];
            }
            set
            {
                var realIndex = jagged.index + indexInJagged;
                data[realIndex] = value;
            }
        }
        public void CopyFrom(JaggedNativeArray<T> source, int targetIndex, int targetParamIndex)
        {
            for (int i = 0; i < source.Length; i++)
            {
                var replacementParamIndexing = source.indexing[i];

                indexing[targetIndex + i] = new JaggedIndexing
                {
                    index = targetParamIndex + replacementParamIndexing.index,
                    length = replacementParamIndexing.length
                };
            }
            for (int i = 0; i < source.data.Length; i++)
            {
                data[targetParamIndex + i] = source.data[i];
            }
        }

        public bool Equals(JaggedNativeArray<T> other)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (!other.data[i].Equals(data[i]))
                {
                    return false;
                }
            }
            for (int i = 0; i < indexing.Length; i++)
            {
                if (!other.indexing[i].Equals(indexing[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public void Dispose()
        {
            this.data.Dispose();
            this.indexing.Dispose();
        }
    }

    public struct JaggedIndexing : IEquatable<JaggedIndexing>
    {
        public int index;
        public ushort length;
        public int Start => index;
        public int End => index + length;

        public T GetValue<T>(NativeArray<T> array, ushort indexInSelf) where T: unmanaged
        {
            return array[indexInSelf + index];
        }

        public bool Equals(JaggedIndexing other)
        {
            return other.index == index && other.length == length;
        }
        public override bool Equals(object obj)
        {
            if (obj is JaggedIndexing indexing)
            {
                return this.Equals(indexing);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return index << 31 | length;
        }
    }

}
