using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    /// <summary>
    /// Used to manage data which is generated as part of a long-running job,
    ///     but also needs to be cached for other systems to access while the job is running
    /// </summary>
    public class NativeDisposableHotSwap<T> : INativeDisposable where T : struct, INativeDisposable
    {
        private bool isActiveDataA;
        private DependencyTracker<T> dataA = null;
        private DependencyTracker<T> dataB = null;

        public bool HasPending { get; private set; }

        public DependencyTracker<T> ActiveData => isActiveDataA ? dataA : dataB;



        /// <summary>
        /// hot swaps to the pending data. Will only do anything if AssignPending was called at some point
        ///     since the last call to this method
        /// </summary>
        public void HotSwapToPending()
        {
            if (HasPending)
            {
                isActiveDataA = !isActiveDataA;
                HasPending = false;
            }
        }
        /// <summary>
        /// assigns the internal pending native collection, and disposes the existing one in the currently pending slot
        ///     if it exists
        /// </summary>
        /// <param name="pendingData"></param>
        public void AssignPending(T pendingData)
        {
            if (HasPending)
            {
                Debug.LogWarning("Assigning Pending value without a hotswap happening. A previously pending value must be disposed");
            }
            HasPending = true;
            if (isActiveDataA)
            {
                if (dataB != null)
                {
                    dataB.Dispose();
                }
                dataB = new DependencyTracker<T>(pendingData);
            }
            else
            {
                if (dataA != null)
                {
                    dataA.Dispose();
                }
                dataA = new DependencyTracker<T>(pendingData);
            }
        }

        public void Dispose()
        {
            dataA?.Dispose();
            dataB?.Dispose();
        }

        public virtual JobHandle Dispose(JobHandle inputDeps)
        {
            if(dataA != null && dataB != null)
            {
                return JobHandle.CombineDependencies(
                    dataA.Dispose(inputDeps),
                    dataB.Dispose(inputDeps));
            }
            if (dataA != null)
            {
                return dataA.Dispose(inputDeps);
            }
            if (dataB != null)
            {
                return dataB.Dispose(inputDeps);
            }
            return default;
        }
    }
}
