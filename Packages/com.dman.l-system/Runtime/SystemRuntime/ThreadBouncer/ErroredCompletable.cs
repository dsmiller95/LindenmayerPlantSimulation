using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class ErrorCompletable<T>: ICompletable<T>
    {
        public JobHandle currentJobHandle => default;

        private string error;
        public ErrorCompletable(string errorMessage)
        {
            this.error = errorMessage;
        }


        public string GetError()
        {
            return error;
        }

        public T GetData()
        {
            return default(T);
        }

        public bool IsComplete()
        {
            return true;
        }

        public bool HasErrored()
        {
            return true;
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
