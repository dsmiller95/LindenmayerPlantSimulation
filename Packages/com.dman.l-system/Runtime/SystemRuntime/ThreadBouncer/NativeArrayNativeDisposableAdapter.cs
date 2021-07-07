using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public struct NativeArrayNativeDisposableAdapter<T>: INativeDisposable where T : unmanaged
    {
        public NativeArray<T> data;

        public NativeArrayNativeDisposableAdapter(NativeArray<T> data)
        {
            this.data = data;
        }

        public void Dispose()
        {
            data.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return data.Dispose(inputDeps);
        }

        public static implicit operator NativeArray<T>(NativeArrayNativeDisposableAdapter<T> wrapper) => wrapper.data;
        public static implicit operator NativeArrayNativeDisposableAdapter<T>(NativeArray<T> data) => new NativeArrayNativeDisposableAdapter<T>(data);
    }
}
