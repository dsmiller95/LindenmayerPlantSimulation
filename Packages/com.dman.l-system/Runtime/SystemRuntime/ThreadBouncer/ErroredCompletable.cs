using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class ErrorCompletable<T> : ICompletable<T>
    {
#if UNITY_EDITOR
        public string TaskDescription => "Error";
#endif
        public JobHandle currentJobHandle => default;

        private string error;
        public ErrorCompletable(string errorMessage)
        {
            error = errorMessage;
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

        public ICompletable StepNext()
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
