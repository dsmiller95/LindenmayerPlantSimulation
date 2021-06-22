using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem.Packages.com.dman.l_system.Runtime.SystemCompiler.Linker
{
    [Serializable]
    public class SerializableList<T>: ISerializationCallbackReceiver, IList<T>
    {
        public List<T> backing;
        [SerializeField]
        private byte[] allFileData;

        public SerializableList(List<T> backingList = null)
        {
            this.backing = backingList ?? new List<T>();
        }
        public void OnAfterDeserialize()
        {
            if (allFileData == null)
            {
                backing = null;
                return;
            }
            using (MemoryStream stream = new MemoryStream(allFileData))
            {
                var resultObject = new BinaryFormatter().Deserialize(stream);
                backing = resultObject as List<T>;
            }

        }

        public void OnBeforeSerialize()
        {
            if (backing == null)
            {
                allFileData = null;
                return;
            }
            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, backing);
                this.allFileData = stream.ToArray();
            }
        }

        #region IList reimplementation

        public T this[int index] { get => ((IList<T>)backing)[index]; set => ((IList<T>)backing)[index] = value; }

        public int Count => ((ICollection<T>)backing).Count;

        public bool IsReadOnly => ((ICollection<T>)backing).IsReadOnly;

        public void Add(T item)
        {
            ((ICollection<T>)backing).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)backing).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)backing).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)backing).CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)backing).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)backing).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)backing).Insert(index, item);
        }


        public bool Remove(T item)
        {
            return ((ICollection<T>)backing).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<T>)backing).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)backing).GetEnumerator();
        }
        #endregion
    }
}
