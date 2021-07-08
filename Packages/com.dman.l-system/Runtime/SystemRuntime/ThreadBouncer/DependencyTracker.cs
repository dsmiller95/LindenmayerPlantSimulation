using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class DependencyTracker<T> : INativeDisposable where T : INativeDisposable
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
    }
}
