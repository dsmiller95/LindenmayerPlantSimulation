using Dman.LSystem.SystemRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public abstract class LSystemCompileTimeParameterGenerator : MonoBehaviour
    {
        public abstract Dictionary<string, string> GenerateCompileTimeParameters();
    }

    public class LSystemBehavior : MonoBehaviour
    {
        /// <summary>
        /// The L System definition to execute
        /// </summary>
        public LSystemObject systemObject;

        /// <summary>
        /// Gets the current state of the system
        /// </summary>
        public SymbolString<float> CurrentState => systemState.currentSymbols;

        /// <summary>
        /// Emits whenever the system state changes
        /// </summary>
        public event Action OnSystemStateUpdated;

        private LSystemState<float> systemState;

        private bool ownsCompiledSystem;
        private LSystem _compiledSystem;
        private ArrayParameterRepresenation<float> runtimeParameters;

        public JobHandle symbolDependents {get; private set;}

        /// <summary>
        /// true if the last system step changed the state of this behavior, false otherwise
        /// </summary>
        public bool lastUpdateChanged { get; private set; }
        /// <summary>
        /// the value of Time.time when this system was last updated
        /// </summary>
        public float lastUpdateTime { get; private set; }
        /// <summary>
        /// the total continuous steps taken by this system
        /// </summary>
        public int totalSteps { get; private set; }

        public void SymbolStringDependsOn(JobHandle dependency)
        {
            symbolDependents = JobHandle.CombineDependencies(symbolDependents, dependency);
        }

        /// <summary>
        /// Assign a new system definition to this behavior.
        /// </summary>
        /// <param name="newSystemObject"></param>
        public void SetSystem(LSystemObject newSystemObject)
        {
            if (systemObject != null)
            {
                systemObject.OnCachedSystemUpdated -= OnSystemObjectRecompiled;
            }
            systemObject = newSystemObject;
            systemObject.OnCachedSystemUpdated += OnSystemObjectRecompiled;
            ResetState();
        }

        private void SetNewCompiledSystem()
        {
            if (ownsCompiledSystem)
            {
                _compiledSystem.Dispose();
            }
            var globalParams = GetComponent<LSystemCompileTimeParameterGenerator>();
            if (globalParams != null)
            {
                Debug.Log("compiling new system");
                var extraGlobalParams = globalParams.GenerateCompileTimeParameters();
                _compiledSystem = systemObject?.CompileWithParameters(extraGlobalParams);
                ownsCompiledSystem = true;
            }
            else
            {
                Debug.Log("using cached system");
                _compiledSystem = systemObject?.compiledSystem;
                ownsCompiledSystem = false;
            }
        }

        /// <summary>
        /// Reset the system state to the Axiom, and re-initialize the Random provider with a random seed unless otherwise specified
        /// </summary>
        public void ResetState(int? newSeed = null)
        {
            if(systemState != null)
            {
                systemState.currentSymbols.Dispose(symbolDependents);
            }

            this.SetNewCompiledSystem();
            totalSteps = 0;
            lastUpdateChanged = true;
            lastUpdateTime = Time.time + UnityEngine.Random.Range(0f, 0.3f);
            systemState = new DefaultLSystemState(systemObject.axiom, newSeed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            // clear out the next state handle. if an update is pending, just abort it.

            OnSystemStateUpdated?.Invoke();
        }

        private LSystemSteppingState pendingStateHandle;

        public bool CanStep()
        {
            return pendingStateHandle == null && symbolDependents.IsCompleted;
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
            if (!CanStep()) {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            this.StartCoroutine(StepSystemCorouting());
        }

        private IEnumerator StepSystemCorouting()
        {
            try
            {
                if (_compiledSystem == null)
                {
                    SetNewCompiledSystem();
                }
                pendingStateHandle = _compiledSystem?.StepSystemJob(systemState, runtimeParameters.GetCurrentParameters());
            }
            catch (System.Exception e)
            {
                lastUpdateChanged = false;
                Debug.LogException(e);
                pendingStateHandle = null;
                yield break;
            }
            LSystemState<float> nextState = null;
            while (nextState == null)
            {
                yield return new WaitForEndOfFrame();
                nextState = pendingStateHandle.StepToNextState();
            }
            pendingStateHandle = null;
            this.RenderNextState(nextState);
        }

        private void RenderNextState(LSystemState<float> nextState)
        {
            lastUpdateChanged = !(systemState?.currentSymbols.Equals(nextState.currentSymbols) ?? false);
            systemState?.currentSymbols.Dispose();

            systemState = nextState;

            OnSystemStateUpdated?.Invoke();

            totalSteps++;
            lastUpdateTime = Time.time + UnityEngine.Random.Range(0, 0.1f);
        }

        /// <summary>
        /// Set a runtime parameter exposed by the system object.
        ///     Will log an error to the console if no parameter by <paramref name="parameterName"/> is exposed by the system
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        public void SetRuntimeParameter(string parameterName, float parameterValue)
        {
            runtimeParameters.SetParameter(parameterName, parameterValue);
        }

        private void Awake()
        {
            lastUpdateTime = Time.time + UnityEngine.Random.Range(.3f, 0.6f);
            if (systemObject != null)
            {
                systemObject.OnCachedSystemUpdated += OnSystemObjectRecompiled;
            }
            totalSteps = 0;
        }

        private void OnDestroy()
        {
            if (systemObject != null)
            {
                systemObject.OnCachedSystemUpdated -= OnSystemObjectRecompiled;
            }
            this.StopAllCoroutines();
            symbolDependents.Complete();
            pendingStateHandle?.ForceCompletePendingJobsAndDeallocate();
            if (ownsCompiledSystem)
            {
                _compiledSystem?.Dispose();
            }
            systemState?.currentSymbols.Dispose();
        }

        private void OnSystemObjectRecompiled()
        {
            runtimeParameters = systemObject.GetRuntimeParameters();
            ResetState();
        }
    }
}
