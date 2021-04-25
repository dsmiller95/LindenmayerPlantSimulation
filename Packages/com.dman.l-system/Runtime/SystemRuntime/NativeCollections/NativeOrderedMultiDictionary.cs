using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    public struct NativeOrderedMultiDictionary<TValue> : IDisposable where TValue: unmanaged
    {
        [NativeDisableParallelForRestriction]
        public JaggedNativeArray<TValue> rawData;
        [ReadOnly]
        public NativeHashMap<int, int> dictionaryToIndex;

        public NativeOrderedMultiDictionary(
            IDictionary<int, TValue[]> data,
            Allocator allocator)
        {
            dictionaryToIndex = new NativeHashMap<int, int>(data.Count, allocator);

            var jaggedData = new List<TValue[]>();
            foreach (var kvp in data)
            {
                dictionaryToIndex[kvp.Key] = jaggedData.Count;
                jaggedData.Add(kvp.Value);
            }
            rawData = new JaggedNativeArray<TValue>(jaggedData.ToArray(), allocator);
        }

        public static NativeOrderedMultiDictionary<TValue> WithMapFunction<TPreValue>(
            IDictionary<int, IList<TPreValue>> data,
            Func<TPreValue, TValue> mapper,
            Allocator allocator)
        {
            var newDictionary = data
                .ToDictionary(x => x.Key, x => x.Value.Select(mapper).ToArray());
            return new NativeOrderedMultiDictionary<TValue>(newDictionary, allocator);
        }

        public bool IsCreated => rawData.IsCreated && dictionaryToIndex.IsCreated;
        
        public bool TryGetValue(int key, out JaggedIndexing value)
        {
            if(dictionaryToIndex.TryGetValue(key, out var index))
            {
                value = rawData[index];
                return true;
            }
            value = default;
            return false;
        }
        
        public JaggedIndexing this[int key]
        {
            get
            {
                var index = dictionaryToIndex[key];
                return rawData[index];
            }
        }
        public TValue this[JaggedIndexing keyIndex, int indexInList]
        {
            get
            {
                return rawData[keyIndex, indexInList];
            }
            set
            {
                rawData[keyIndex, indexInList] = value;
            }
        }
        public TValue this[int key, int indexInList]
        {
            get
            {
                return this[this[key], indexInList];
            }
            set
            {
                this[this[key], indexInList] = value;
            }
        }
        public void Dispose()
        {
            this.rawData.Dispose();
            this.dictionaryToIndex.Dispose();
        }
    }
}
