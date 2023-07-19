using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    
    public static class CompleteableToUniTask
    {
        public static async UniTask<T> ToUniTask<T>(
            this ICompletable<T> completable,
            CancellationToken forceSynchronous, 
            CancellationToken cancel = default)
        {
            try
            {
                while (!completable.IsComplete())
                {
                    if (!forceSynchronous.IsCancellationRequested)
                    {
                        using var cancelJobSource = CancellationTokenSource.CreateLinkedTokenSource(forceSynchronous, cancel);
                        var cancelled = await UniTask.WaitUntil(
                                () => completable.currentJobHandle.IsCompleted,
                                cancellationToken: cancelJobSource.Token)
                            .SuppressCancellationThrow();
                        if (cancelled && cancel.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                    }
                    if (completable.HasErrored())
                    {
                        throw new LSystemRuntimeException(completable.GetError());
                    }
                    completable = completable.StepNextTyped();
                }

                return completable.GetData();
            }
            finally
            {
                completable.Dispose();
            }
        }
        
        /// <summary>
        /// forces the completable to synchronously complete
        /// </summary>
        /// <param name="completable"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="CompletableException"></exception>
        public static T ToCompleted<T>(this ICompletable<T> completable)
        {
            var cancelledSource = new CancellationTokenSource();
            cancelledSource.Cancel();
            var task = completable.ToUniTask(cancelledSource.Token);
            return task.ExtractSync();
        }

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
            }
            handle.Complete();
            return cancel.IsCancellationRequested;
        }
    }
}
