using System;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace
{
    [BurstCompile]
    public struct NativeArrayAddSubtractJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> addArray;
        [ReadOnly]
        public NativeArray<float> subtractArray;

        public NativeArray<float> writeArray;

        public void Execute(int index)
        {
            writeArray[index] = Mathf.Max(writeArray[index] + addArray[index] - subtractArray[index], 0);
        }
    }
    public struct NativeArraySubtractNegativeProtectionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> subtractArray;

        public NativeArray<float> writeArray;

        public void Execute(int index)
        {
            writeArray[index] = Mathf.Max(writeArray[index] - subtractArray[index], 0);
        }
    }
    [BurstCompile]
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
