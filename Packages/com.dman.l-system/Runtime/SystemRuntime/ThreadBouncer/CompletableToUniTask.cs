using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    
    public static class CompleteableToUniTask
    {
        public static T ExtractSync<T>(this UniTask<T> syncTask)
        {
            Assert.AreEqual(UniTaskStatus.Succeeded, syncTask.Status);
            return syncTask.GetAwaiter().GetResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="cancel"></param>
        /// <param name="maxFameDelay"></param>
        /// <returns>true if cancelled</returns>
        /// <exception cref="OperationCanceledException"></exception>
        public static async UniTask<bool> AwaitCompleteImmediateOnCancel(
            this JobHandle handle,
            CancellationToken cancel,
            bool forceCompleteOnNextTick,
            int maxFameDelay = -1)
        {
            if (cancel.IsCancellationRequested)
            {
                handle.Complete();
                return false;
            }

            var initialFrame = UnityEngine.Time.frameCount;
            while (
                !handle.IsCompleted && 
                !cancel.IsCancellationRequested && 
                (maxFameDelay < 0 || (initialFrame + maxFameDelay) > UnityEngine.Time.frameCount))
            {
                await UniTask.WhenAny(
                    UniTask.Yield(PlayerLoopTiming.PreUpdate).ToUniTask(),
                    UniTask.Yield(PlayerLoopTiming.Update).ToUniTask(),
                    UniTask.Yield(PlayerLoopTiming.PostLateUpdate).ToUniTask()
                ).AttachExternalCancellation(cancel).SuppressCancellationThrow();
                if (forceCompleteOnNextTick)
                {
                    break;
                }
            }
            handle.Complete();
            return cancel.IsCancellationRequested;
        }
    }
}
