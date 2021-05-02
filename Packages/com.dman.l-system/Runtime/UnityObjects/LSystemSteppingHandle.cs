﻿using System;
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

        // metadata
        public int totalSteps { get; private set; }
        public bool lastUpdateChanged { get; private set; }

        /// <summary>
        /// Emits whenever the system state changes
        /// </summary>
        public event Action OnSystemStateUpdated;

        private MonoBehaviour coroutineOwner;

        private LSystem compiledSystem;
        public ArrayParameterRepresenation<float> runtimeParameters { get; private set; }

        public LSystemState<float> currentState { get; private set; }
        private LSystemState<float> lastState;

        private LSystemSteppingState pendingStateHandle;
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
            this.ResetState(
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
            this.ResetState(
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
            this.ResetState(
                new DefaultLSystemState(mySystemObject.axiom, UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                mySystemObject.compiledSystem);
        }

        /// <summary>
        /// Optionally Set to a different L-system, and reset the state and the current runtime parameters
        /// </summary>
        private void ResetState(
           DefaultLSystemState newState,
           LSystem newSystem)
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
                this.SetNewCompiledSystem(newSystem);
            }

            // clear out the next state handle. if an update is pending, just abort it.

            OnSystemStateUpdated?.Invoke();
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

        public void Dispose()
        {
            if (useSharedSystem)
            {
                this.mySystemObject.OnCachedSystemUpdated -= OnSharedSystemRecompiled;
            }
            if (pendingCoroutine != null)
            {
                coroutineOwner.StopCoroutine(pendingCoroutine);
            }
            if (pendingStateHandle != null)
            {
                pendingStateHandle?.ForceCompletePendingJobsAndDeallocate();
                pendingStateHandle = null;
            }
            lastState?.currentSymbols.Dispose();
            lastState = null;

            currentState?.currentSymbols.Dispose();
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
            lastState?.currentSymbols.Dispose();

            lastState = currentState;
            currentState = nextState;

            lastUpdateChanged = !(currentState?.currentSymbols.Data.Equals(lastState.currentSymbols.Data) ?? false);
            totalSteps++;

            pendingStateHandle = null;
            pendingCoroutine = null;
            OnSystemStateUpdated?.Invoke();
        }


        private void SetNewCompiledSystem(LSystem newSystem)
        {
            if (!this.useSharedSystem)
            {
                compiledSystem?.Dispose();
            }
            this.compiledSystem = newSystem;
        }
    }
}
