
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class CompletableExecutor : MonoBehaviour
    {
        public static CompletableExecutor Instance { get; private set; }
        private IList<CompletableHandle> PendingCompletables = new List<CompletableHandle>();

        public CompletableHandle<T> RegisterCompletable<T>(ICompletable<T> completable)
        {
            var handle = new CompletableHandle<T>(completable);
            PendingCompletables.Add(handle);
            return handle;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            DoSteps();
        }
        private void LateUpdate()
        {
            DoSteps();
        }

        private void DoSteps()
        {
            var completablesToStep = new List<int>();
            for (int i = 0; i < PendingCompletables.Count; i++)
            {
                var completable = PendingCompletables[i];
                if (!completable.AreAllJobsCompleted())
                {
                    completablesToStep.Add(i);
                    continue;
                }
                var shouldKeep = this.DoSafeStep(completable);
                if (!shouldKeep)
                {
                    PendingCompletables.RemoveAt(i);
                    i--;
                }
            }
            UnityEngine.Profiling.Profiler.BeginSample("Completable force step");
            completablesToStep.Reverse();
            foreach (var completableIndex in completablesToStep)
            {
                var completable = PendingCompletables[completableIndex];
                var shouldKeep = this.DoSafeStep(completable);
                if (!shouldKeep)
                {
                    PendingCompletables.RemoveAt(completableIndex);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private bool DoSafeStep(CompletableHandle x)
        {
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("stepping " + x.TaskDescription);
#endif
            try
            {
                return x.TryStep();
            }
            catch (System.Exception e1)
            {
                Debug.LogException(e1);
                try
                {
                    x.Cancel();
                }
                catch (System.Exception e2)
                {
                    Debug.LogException(e2);
                }
                return false;
            }
#if UNITY_EDITOR
            finally
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
#endif
        }

        private void OnDestroy()
        {
            foreach (var completable in PendingCompletables)
            {
                completable.Dispose();
            }
        }
    }
}
