using Dman.LSystem.SystemRuntime;
using System;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class LSystemBehavior : MonoBehaviour
    {
        /// <summary>
        /// The L System definition to execute
        /// </summary>
        public LSystemObject systemObject;

        /// <summary>
        /// Gets the current state of the system
        /// </summary>
        public SymbolString<double> CurrentState => systemState.currentSymbols;

        /// <summary>
        /// Emits whenever the system state changes
        /// </summary>
        public event Action OnSystemStateUpdated;

        private LSystemState<double> systemState;
        private ArrayParameterRepresenation<double> runtimeParameters;

        private SymbolString<double> lastState;
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

        /// <summary>
        /// Assign a new system definition to this behavior.
        /// </summary>
        /// <param name="newSystemObject"></param>
        public void SetSystem(LSystemObject newSystemObject)
        {
            if (systemObject != null)
            {
                systemObject.OnSystemUpdated -= OnSystemObjectRecompiled;
            }
            systemObject = newSystemObject;
            systemObject.OnSystemUpdated += OnSystemObjectRecompiled;
            ResetState();
        }

        /// <summary>
        /// Reset the system state to the Axiom, and re-initialize the Random provider with a random seed unless otherwise specified
        /// </summary>
        public void ResetState(int? newSeed = null)
        {
            lastState = null;
            totalSteps = 0;
            lastUpdateChanged = true;
            lastUpdateTime = Time.time + UnityEngine.Random.Range(0f, 0.3f);
            systemState = new DefaultLSystemState(systemObject.axiom, newSeed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            OnSystemStateUpdated?.Invoke();
        }


        /// <summary>
        /// step the Lsystem forward one tick
        /// </summary>
        /// <returns>true if the state changed. false otherwise</returns>
        public void StepSystem()
        {
            try
            {
                systemState = systemObject.compiledSystem?.StepSystem(systemState, runtimeParameters.GetCurrentParameters());
                Debug.Log($"len: {systemState.currentSymbols.Length} iter:{totalSteps}");
                Debug.Log(systemState.currentSymbols);
            }
            catch (System.Exception e)
            {
                lastUpdateChanged = false;
                Debug.LogException(e);
                return;
            }
            OnSystemStateUpdated?.Invoke();

            lastUpdateChanged = !(lastState?.Equals(CurrentState) ?? false);
            lastState = CurrentState;
            totalSteps++;
            lastUpdateTime = Time.time + UnityEngine.Random.Range(0, 0.1f);
        }

        /// <summary>
        /// Set a runtime parameter exposed by the system object.
        ///     Will log an error to the console if no parameter by <paramref name="parameterName"/> is exposed by the system
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        public void SetRuntimeParameter(string parameterName, double parameterValue)
        {
            runtimeParameters.SetParameter(parameterName, parameterValue);
        }

        private void Awake()
        {
            lastUpdateTime = Time.time + UnityEngine.Random.Range(.3f, 0.6f);
            if (systemObject != null)
            {
                systemObject.OnSystemUpdated += OnSystemObjectRecompiled;
            }
            totalSteps = 0;
        }

        private void OnDestroy()
        {
            if (systemObject != null)
            {
                systemObject.OnSystemUpdated += OnSystemObjectRecompiled;
            }
        }

        private void OnSystemObjectRecompiled()
        {
            lastState = null;
            totalSteps = 0;
            lastUpdateChanged = true;
            lastUpdateTime = Time.time + UnityEngine.Random.Range(0f, 0.3f);
            systemState = new DefaultLSystemState(systemObject.axiom, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            runtimeParameters = systemObject.GetRuntimeParameters();
            OnSystemStateUpdated?.Invoke();
        }
    }
}
