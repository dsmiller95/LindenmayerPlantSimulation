
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
            PendingCompletables = PendingCompletables
                .Where(x =>
                {
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
                })
                .ToList();
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
