using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime
{
    public class DependencyTracker<T> : INativeDisposable where T : INativeDisposable
    {
        public T Data { get; private set; }
        private JobHandle dependencies;

        public DependencyTracker(T data, JobHandle deps = default)
        {
            dependencies = deps;
            Data = data;
        }

        public void RegisterDependencyOnData(JobHandle deps)
        {
            dependencies = JobHandle.CombineDependencies(deps, dependencies);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return Data.Dispose(
                JobHandle.CombineDependencies(
                    inputDeps,
                    dependencies)
                );
        }

        public void Dispose()
        {
            if (dependencies.Equals(default(JobHandle)))
            {
                DisposeImmediate();
                return;
            }
            Data.Dispose(dependencies);
        }
        public void DisposeImmediate()
        {
            Data.Dispose();
        }
    }
}
