using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct NativeOrderedMultiDictionary<TValue> : IDisposable where TValue: unmanaged
    {
        [NativeDisableParallelForRestriction]
        public JaggedNativeArray<TValue> rawData;
        public NativeHashMap<int, int> dictionaryToIndex;

        public NativeOrderedMultiDictionary(IDictionary<int, IList<TValue>> data, Allocator allocator)
        {
            dictionaryToIndex = new NativeHashMap<int, int>(data.Count, allocator);

            var jaggedData = new List<TValue[]>();
            foreach (var kvp in data)
            {
                dictionaryToIndex[kvp.Key] = jaggedData.Count;
                jaggedData.Add(kvp.Value.ToArray());
            }
            rawData = new JaggedNativeArray<TValue>(jaggedData.ToArray(), allocator);
        }

        public bool IsCreated => rawData.IsCreated && dictionaryToIndex.IsCreated;
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
