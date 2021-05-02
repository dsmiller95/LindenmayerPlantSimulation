using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class LSystemSteppingHandle: IDisposable
    {
        public JobHandle currentStateSymbolDependents { get; private set; }

        // metadata
        public int totalSteps { get; private set; }
        public bool lastUpdateChanged { get; private set; }

        /// <summary>
        /// Emits whenever the system state changes
        /// </summary>
        public event Action OnSystemStateUpdated;

        private MonoBehaviour coroutineOwner;

        private LSystem compiledSystem;
        private bool ownsSystem;

        public LSystemState<float> currentState { get; private set; }
        private LSystemState<float> lastState;
        private JobHandle lastStateSymbolDependents;

        private LSystemSteppingState pendingStateHandle;
        private IEnumerator pendingCoroutine;


        public LSystemSteppingHandle(MonoBehaviour coroutineOwner)
        {
            totalSteps = 0;
            lastUpdateChanged = true;
            this.coroutineOwner = coroutineOwner;
        }

        public void RegisterDependencyForSymbols(JobHandle dependency)
        {
            currentStateSymbolDependents = JobHandle.CombineDependencies(
                currentStateSymbolDependents,
                dependency
                );
        }


        /// <summary>
        /// Reset the system state to the Axiom, and re-initialize the Random provider with a random seed unless otherwise specified
        /// </summary>
        public void ResetState(
            DefaultLSystemState newState,
            LSystem newSystem = null,
            bool ownNewSystem = true)
        {
            if(pendingCoroutine != null)
            {
                coroutineOwner.StopCoroutine(pendingCoroutine);
            }
            if (pendingStateHandle != null)
            {
                pendingStateHandle?.ForceCompletePendingJobsAndDeallocate();
                pendingStateHandle = null;
            }
            lastState?.currentSymbols.Dispose(lastStateSymbolDependents);
            lastState = null;
            currentState?.currentSymbols.Dispose(currentStateSymbolDependents);
            currentState = null;

            this.SetNewCompiledSystem(newSystem, ownNewSystem);

            totalSteps = 0;
            lastUpdateChanged = true;
            currentState = newState;
            // clear out the next state handle. if an update is pending, just abort it.

            OnSystemStateUpdated?.Invoke();
        }

        public bool CanStep()
        {
            return pendingStateHandle == null && currentStateSymbolDependents.IsCompleted;
        }

        /// <summary>
        /// step the Lsystem forward one tick. when CompleteInLateUpdate is true, be very careful with changes to the L-system
        ///     it is not perfectly protected against threading race conditions, so be sure not to make any mutations to 
        ///     the L-system while the behaviors are executing.
        /// </summary>
        /// <param name="CompleteInLateUpdate">When set to true, the behavior will queue up the jobs and wait until the next frame to complete them</param>
        /// <returns>true if the state changed. false otherwise</returns>
        public void StepSystem(
            ArrayParameterRepresenation<float> runtimeParameters)
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            pendingCoroutine = StepSystemCoroutine(runtimeParameters);
            coroutineOwner.StartCoroutine(pendingCoroutine);
        }

        public void Dispose()
        {
            if (pendingCoroutine != null)
            {
                coroutineOwner.StopCoroutine(pendingCoroutine);
            }
            if (pendingStateHandle != null)
            {
                pendingStateHandle?.ForceCompletePendingJobsAndDeallocate();
                pendingStateHandle = null;
            }
            lastStateSymbolDependents.Complete();
            lastState?.currentSymbols.Dispose(lastStateSymbolDependents);
            lastState = null;

            currentStateSymbolDependents.Complete();
            currentState?.currentSymbols.Dispose(currentStateSymbolDependents);
            currentState = null;
        }

        private IEnumerator StepSystemCoroutine(ArrayParameterRepresenation<float> runtimeParameters)
        {
            try
            {
                if (compiledSystem == null || compiledSystem.isDisposed)
                {
                    Debug.LogError("No Compiled system available!");
                    yield break;
                }
                pendingStateHandle = compiledSystem.StepSystemJob(currentState, runtimeParameters.GetCurrentParameters());
            }
            catch (System.Exception e)
            {
                lastUpdateChanged = false;
                Debug.LogException(e);
                pendingStateHandle = null;
                yield break;
            }
            if (pendingStateHandle == null)
            {
                yield break;
            }
            LSystemState<float> nextState = null;
            var waitAtEndOfFrame = false;
            while (nextState == null)
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
                nextState = pendingStateHandle.StepToNextState();
            }

            // dispose the last state
            lastStateSymbolDependents.Complete();
            lastState?.currentSymbols.Dispose();
            lastStateSymbolDependents = currentStateSymbolDependents;

            lastState = currentState;
            currentState = nextState;

            lastUpdateChanged = !(currentState?.currentSymbols.Equals(lastState.currentSymbols) ?? false);
            totalSteps++;

            pendingStateHandle = null;
            pendingCoroutine = null;
            OnSystemStateUpdated?.Invoke();
        }


        private void SetNewCompiledSystem(LSystem newSystem, bool ownNewSystem)
        {
            if (this.ownsSystem)
            {
                compiledSystem.Dispose();
            }
            this.compiledSystem = newSystem;
            this.ownsSystem = ownNewSystem;
        }
    }
}
