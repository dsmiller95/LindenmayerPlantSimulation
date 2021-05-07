
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public class CompletableExecutor: MonoBehaviour
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
            PendingCompletables = PendingCompletables
                .Where(x => x.TryStep())
                .ToList();
        }
        private void LateUpdate()
        {
            PendingCompletables = PendingCompletables
                .Where(x => x.TryStep())
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
