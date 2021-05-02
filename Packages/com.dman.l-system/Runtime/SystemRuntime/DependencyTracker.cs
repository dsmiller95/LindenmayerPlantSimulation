using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime
{
    public class DependencyTracker<T>: INativeDisposable where T: INativeDisposable
    {
        public T Data { get; private set; }
        private JobHandle dependencies;

        public DependencyTracker(T data, JobHandle deps = default)
        {
            dependencies = deps;
            this.Data = data;
        }

        public void RegisterDependencyOnData(JobHandle deps)
        {
            this.dependencies = JobHandle.CombineDependencies(deps, dependencies);
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
            if(dependencies.Equals(default(JobHandle)))
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
