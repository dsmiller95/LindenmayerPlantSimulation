using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class LSystemSteppingHandle : IDisposable
    {
        // metadata
        public int totalSteps { get; private set; }
        public bool lastUpdateChanged { get; private set; }

        /// <summary>
        /// Emits whenever the system state changes
        /// </summary>
        public event Action OnSystemStateUpdated;

        private MonoBehaviour coroutineOwner;

        private LSystemStepper compiledSystem;
        public ArrayParameterRepresenation<float> runtimeParameters { get; private set; }

        public LSystemState<float> currentState { get; private set; }
        private LSystemState<float> lastState;

        private ICompletable<LSystemState<float>> pendingStateHandle;
        private IEnumerator pendingCoroutine;
        private LSystemObject mySystemObject;
        private bool useSharedSystem;

        public LSystemSteppingHandle(
            MonoBehaviour coroutineOwner,
            LSystemObject mySystemObject,
            bool useSharedSystem)
        {
            totalSteps = 0;
            lastUpdateChanged = true;
            this.coroutineOwner = coroutineOwner;

            this.mySystemObject = mySystemObject;
            this.useSharedSystem = useSharedSystem;

            if (useSharedSystem)
            {
                this.mySystemObject.OnCachedSystemUpdated += OnSharedSystemRecompiled;
            }
        }


        /// <summary>
        /// Sets the system state to the new state. will clear out all metadata
        ///     related to how the current state has been updated, and all changes to runtime parameters
        /// </summary>
        public void ResetState()
        {
            ResetState(
                new DefaultLSystemState(mySystemObject.axiom, UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                null);
        }

        public void RecompileLSystem(Dictionary<string, string> globalCompileTimeOverrides)
        {
            if (useSharedSystem)
            {
                Debug.LogError("Invalid operation, when using shared system must compile via the shared object handle");
                return;
            }
            var newSystem = mySystemObject.CompileWithParameters(globalCompileTimeOverrides);
            ResetState(
                new DefaultLSystemState(mySystemObject.axiom, UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                newSystem);
        }
        private void OnSharedSystemRecompiled()
        {
            if (!useSharedSystem)
            {
                Debug.LogError("Invalid operation. should only listen for recompilation updates when using shared system");
                return;
            }
            ResetState(
                new DefaultLSystemState(mySystemObject.axiom, UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                mySystemObject.compiledSystem);
        }

        /// <summary>
        /// Optionally Set to a different L-system, and reset the state and the current runtime parameters
        /// </summary>
        private void ResetState(
           DefaultLSystemState newState,
           LSystemStepper newSystem)
        {
            if (pendingCoroutine != null)
            {
                coroutineOwner.StopCoroutine(pendingCoroutine);
            }
            if (pendingStateHandle != null)
            {
                pendingStateHandle?.Dispose();
                pendingStateHandle = null;
            }
            lastState?.currentSymbols.Dispose();
            lastState = null;
            currentState?.currentSymbols.Dispose();
            currentState = null;

            totalSteps = 0;
            lastUpdateChanged = true;
            currentState = newState;

            runtimeParameters = mySystemObject.GetRuntimeParameters();

            if (newSystem != null)
            {
                SetNewCompiledSystem(newSystem);
            }

            // clear out the next state handle. if an update is pending, just abort it.

            OnSystemStateUpdated?.Invoke();
        }

        public bool HasValidSystem()
        {
            return compiledSystem != null;
        }
        public bool CanStep()
        {
            return pendingStateHandle == null;
        }

        /// <summary>
        /// step the Lsystem forward one tick. when CompleteInLateUpdate is true, be very careful with changes to the L-system
        ///     it is not perfectly protected against threading race conditions, so be sure not to make any mutations to 
        ///     the L-system while the behaviors are executing.
        /// </summary>
        /// <param name="CompleteInLateUpdate">When set to true, the behavior will queue up the jobs and wait until the next frame to complete them</param>
        /// <returns>true if the state changed. false otherwise</returns>
        public void StepSystem()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            pendingCoroutine = StepSystemCoroutine(runtimeParameters);
            coroutineOwner.StartCoroutine(pendingCoroutine);
        }
        public void StepSystemImmediate()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            var localRoutine = pendingCoroutine = StepSystemCoroutine(runtimeParameters);
            while (localRoutine.MoveNext());
        }

        public void RepeatLastStepImmediate()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            var localRoutine = pendingCoroutine = StepSystemCoroutine(runtimeParameters, true);
            while (localRoutine.MoveNext()) ;
        }


        private IEnumerator StepSystemCoroutine(
            ArrayParameterRepresenation<float> runtimeParameters,
            bool repeatLast = false)
        {
            try
            {
                if (compiledSystem == null || compiledSystem.isDisposed)
                {
                    Debug.LogError("No Compiled system available!");
                    yield break;
                }
                if (repeatLast)
                {
                    pendingStateHandle = compiledSystem.StepSystemJob(lastState, runtimeParameters.GetCurrentParameters());
                }
                else
                {
                    pendingStateHandle = compiledSystem.StepSystemJob(currentState, runtimeParameters.GetCurrentParameters());
                }
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
            var waitAtEndOfFrame = false;
            while (!pendingStateHandle.IsComplete())
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
                pendingStateHandle = pendingStateHandle.StepNext();
            }
            var nextState = pendingStateHandle.GetData();
            if (repeatLast)
            {
                // dispose the current state, since it is about to be replaced
                currentState?.currentSymbols.Dispose();
            }
            else
            {
                // dispose the last state
                lastState?.currentSymbols.Dispose();
                lastState = currentState;
                totalSteps++;
            }
            currentState = nextState;
            lastUpdateChanged = !(currentState?.currentSymbols.Data.Equals(lastState.currentSymbols.Data) ?? false);

            pendingStateHandle = null;
            pendingCoroutine = null;
            OnSystemStateUpdated?.Invoke();
        }


        private void SetNewCompiledSystem(LSystemStepper newSystem)
        {
            if (!useSharedSystem)
            {
                compiledSystem?.Dispose();
            }
            compiledSystem = newSystem;
        }
        public void Dispose()
        {
            if (useSharedSystem)
            {
                mySystemObject.OnCachedSystemUpdated -= OnSharedSystemRecompiled;
            }
            if (pendingCoroutine != null)
            {
                coroutineOwner.StopCoroutine(pendingCoroutine);
            }
            if (pendingStateHandle != null)
            {
                pendingStateHandle?.Dispose();
                pendingStateHandle = null;
            }
            lastState?.currentSymbols.Dispose();
            lastState = null;

            currentState?.currentSymbols.Dispose();
            currentState = null;
        }

    }
}
