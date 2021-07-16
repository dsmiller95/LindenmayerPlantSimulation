using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public interface ICompletable : INativeDisposable
    {
        /// <summary>
        /// the jobhandle which this completable is waiting on
        /// </summary>
        public JobHandle currentJobHandle { get; }
        /// <summary>
        /// Complete the currently pending job handle, and kick off the next
        ///     series of jobs from the main thread
        /// </summary>
        /// <returns></returns>
        public ICompletable StepNext();

        /// <summary>
        /// Return true if this completable has the final product <see cref="T"/>.
        ///     false if this completable is handling a currently pending job.
        /// </summary>
        /// <returns></returns>
        public bool IsComplete();
        /// <summary>
        /// Return true if this completable has completed due to a runtime error
        /// </summary>
        /// <returns></returns>
        public bool HasErrored();
        public string GetError();

#if UNITY_EDITOR
        public string TaskDescription { get; }
#endif
    }
    public interface ICompletable<T> : ICompletable
    {
        /// <summary>
        /// Get the completed data. if <see cref="IsComplete"/> is false, or if HasErrored is true, will return null.
        /// </summary>
        /// <returns></returns>
        public T GetData();
    }
}
