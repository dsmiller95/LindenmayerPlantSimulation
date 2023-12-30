using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    public struct NativeArrayNativeDisposableAdapter<T> : INativeDisposable where T : unmanaged
    {
        public NativeArray<T> data;

        public NativeArrayNativeDisposableAdapter(NativeArray<T> data)
        {
            this.data = data;
        }

        public void Dispose()
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (data.IsCreated)
            {
                return data.Dispose(inputDeps);
            }else
            {
                return default;
            }
        }

        public static implicit operator NativeArray<T>(NativeArrayNativeDisposableAdapter<T> wrapper)
        {
            return wrapper.data;
        }

        public static implicit operator NativeArrayNativeDisposableAdapter<T>(NativeArray<T> data)
        {
            return new NativeArrayNativeDisposableAdapter<T>(data);
        }
    }
}
