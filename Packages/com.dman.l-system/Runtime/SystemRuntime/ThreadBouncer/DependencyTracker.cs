using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    [System.Serializable]
    public class DependencyTracker<T> : ISerializable, INativeDisposable where T : INativeDisposable
    {
        public T Data { get; private set; }
        public bool IsDisposed { get; private set; }
        private JobHandle dependencies;

        public DependencyTracker(T data, JobHandle deps = default)
        {
            dependencies = deps;
            Data = data;
            IsDisposed = false;
        }

        public void RegisterDependencyOnData(JobHandle deps)
        {
            if (IsDisposed) throw new System.Exception("Cannot depend on data. already disposed");
            dependencies = JobHandle.CombineDependencies(deps, dependencies);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsDisposed) return inputDeps;
            var dep = Data.Dispose(
                JobHandle.CombineDependencies(
                    inputDeps,
                    dependencies)
                );
            IsDisposed = true;
            return dep;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            // TODO: can we not force complete here?
            dependencies.Complete();
            Data.Dispose(dependencies);
            IsDisposed = true;
        }
        public void DisposeImmediate()
        {
            if (IsDisposed) return;
            dependencies.Complete();
            Data.Dispose();
            IsDisposed = true;
        }

        #region Serialization

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (!IsDisposed)
            {
                dependencies.Complete();
                info.AddValue("Data", Data);
            }
            info.AddValue("IsDisposed", IsDisposed);
        }


        // The special constructor is used to deserialize values.
        private DependencyTracker(SerializationInfo info, StreamingContext context)
        {
            IsDisposed = info.GetBoolean("IsDisposed");
            dependencies = default;
            if (!IsDisposed)
            {
                Data = info.GetValue<T>("Data");
            }
        }
        #endregion
    }
}
