using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class CompleteCompletable<T> : ICompletable<T>
    {
#if UNITY_EDITOR
        public string TaskDescription => "Complete";
#endif
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

        public string GetError()
        {
            return null;
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
