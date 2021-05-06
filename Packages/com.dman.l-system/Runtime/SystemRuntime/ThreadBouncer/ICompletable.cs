using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public interface ICompletable<T>: INativeDisposable
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
        public ICompletable<T> StepNext();

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

        /// <summary>
        /// Get the completed data. if <see cref="IsComplete"/> is false, or if HasErrored is true, will return null.
        /// </summary>
        /// <returns></returns>
        public T GetData();
    }
}
