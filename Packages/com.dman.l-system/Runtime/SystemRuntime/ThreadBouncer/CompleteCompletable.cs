using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class CompleteCompletable<T>: ICompletable<T>
    {
        public JobHandle currentJobHandle => default;

        private T data;
        public CompleteCompletable(T data)
        {
            this.data = data;
        }

        public T GetData()
        {
            return data;
        }

        public bool IsComplete()
        {
            return true;
        }

        public bool HasErrored()
        {
            return false;
        }

        public ICompletable<T> StepNext()
        {
            return this;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return inputDeps;
        }

        public void Dispose()
        {
        }
    }
}
