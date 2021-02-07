using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public class ArrayParameterRepresenation<T>
    {
        private T[] parameterArray;
        private Dictionary<string, int> parameterNameToIndex;

        public static ArrayParameterRepresenation<T> GenerateFromList<K>(IList<K> defaultParams, Func<K, string> nameSelector, Func<K, T> valueSelector)
        {
            var parameterNameToIndex = new Dictionary<string, int>();
            var defaultValues = new T[defaultParams.Count];
            for (int i = 0; i < defaultParams.Count; i++)
            {
                var param = defaultParams[i];
                defaultValues[i] = valueSelector(param);
                parameterNameToIndex[nameSelector(param)] = i;
            }
            return new ArrayParameterRepresenation<T>
            {
                parameterArray = defaultValues,
                parameterNameToIndex = parameterNameToIndex
            };
        }
        private ArrayParameterRepresenation() { }

        public T[] GetCurrentParameters()
        {
            return parameterArray;
        }

        public void SetParameter(string name, T value)
        {
            if (!parameterNameToIndex.TryGetValue(name, out var parameterIndex))
            {
                Debug.LogError($"Parameter representation does not contain a parameter named {name}");
                return;
            }
            parameterArray[parameterIndex] = value;
        }
    }
}
