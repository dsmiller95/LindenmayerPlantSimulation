using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public static class CompletableExtensions
    {
        public static IEnumerator AsCoroutine<T>(this ICompletable<T> completable, Action<T> OnCompleted = null)
        {
            var waitAtEndOfFrame = false;
            while (!completable.IsComplete())
            {
                waitAtEndOfFrame = !waitAtEndOfFrame;
                if (waitAtEndOfFrame)
                {
                    yield return new WaitForEndOfFrame();
                }
                else
                {
                    yield return null;
                }
                completable = completable.StepNext();
            }
            var result = completable.GetData();
            OnCompleted?.Invoke(result);
        }
    }
}
