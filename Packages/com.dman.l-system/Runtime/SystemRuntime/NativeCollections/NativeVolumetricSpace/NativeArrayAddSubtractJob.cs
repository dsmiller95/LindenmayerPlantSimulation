using System;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace
{
    public struct NativeArrayAddSubtractJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> addArray;
        [ReadOnly]
        public NativeArray<float> subtractArray;

        public NativeArray<float> writeArray;

        public void Execute(int index)
        {
            writeArray[index] += addArray[index] - subtractArray[index];
        }
    }
    public struct NativeArraySubtractJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> subtractArray;

        public NativeArray<float> writeArray;

        public void Execute(int index)
        {
            writeArray[index] += subtractArray[index];
        }
    }
    public struct NativeArrayClearJob : IJobParallelFor
    {
        public float newValue;
        [WriteOnly]
        public NativeArray<float> writeArray;

        public void Execute(int index)
        {
            writeArray[index] = newValue;
        }
    }
}
