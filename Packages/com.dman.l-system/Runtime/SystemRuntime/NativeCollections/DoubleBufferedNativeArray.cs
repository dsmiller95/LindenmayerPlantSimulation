using Dman.LSystem.SystemRuntime.VolumetricData;
using System;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace
{
    public class DoubleBuffered<T> where T : unmanaged 
    {
        public NativeArray<T> a;
        public NativeArray<T> b;

        public bool lastWriteToA;

        public NativeArray<T> CurrentData => lastWriteToA ? a : b;
        public NativeArray<T>  NextData => lastWriteToA ? b : a;

        public DoubleBuffered(NativeArray<T> currentData, NativeArray<T> workingData)
        {
            a = currentData;
            b = workingData;
            lastWriteToA = true;
        }

        public void Swap()
        {
            lastWriteToA = !lastWriteToA;
        }

        public void Dispose()
        {
            a.Dispose();
            b.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                a.Dispose(inputDeps),
                b.Dispose(inputDeps)
                );
        }
    }
}
