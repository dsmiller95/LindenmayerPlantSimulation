using System;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class CompletableHandle : INativeDisposable
    {
        protected ICompletable currentState;
        protected bool IsDisposed = false;

        public CompletableHandle(ICompletable firstState)
        {
            currentState = firstState;
        }

        /// <summary>
        /// Try to step the state forward. returns false if done stepping
        /// </summary>
        /// <returns></returns>
        public virtual bool TryStep()
        {
            if (currentState.IsComplete() || IsDisposed)
            {
                return false;
            }
            currentState = currentState.StepNext();
            return true;
        }
#if UNITY_EDITOR
        public string TaskDescription => currentState?.TaskDescription ?? "";
#endif

        public void Cancel()
        {
            Dispose();
        }

        public bool IsComplete()
        {
            return currentState.IsComplete() || currentState.HasErrored();
        }

        public bool HasData()
        {
            return currentState.IsComplete() && !currentState.HasErrored();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsDisposed) return inputDeps;
            IsDisposed = true;
            return currentState.Dispose(inputDeps);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            currentState.Dispose();
        }

        public void CompleteImmediate()
        {
            while (TryStep()) ;
        }
    }
    public class CompletableHandle<T> : CompletableHandle
    {
        public event Action<T> OnCompleted;
        public CompletableHandle(ICompletable<T> firstState) : base(firstState)
        {
        }

        public override bool TryStep()
        {
            var baseStepped = base.TryStep();
            if (baseStepped && base.HasData())
            {
                OnCompleted?.Invoke(Data());
            }
            return baseStepped;
        }

        public T Data()
        {
            if (!HasData())
            {
                return default(T);
            }
            return ((ICompletable<T>)currentState).GetData();
        }
    }
}
