using Dman.LSystem.SystemRuntime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public class LSystemBehavior : MonoBehaviour
    {
        public LSystemObject systemObject;

        public DefaultLSystemState systemState;

        public SymbolString<double> CurrentState => systemState.currentSymbols;

        public event Action OnSystemStateUpdated;

        private double[] systemParameters;
        private Dictionary<string, int> parameterNameToIndex;

        private SymbolString<double> lastState;
        public bool lastUpdateChanged { get; private set; }
        public float lastUpdateTime { get; private set; }
        public int totalSteps { get; private set; }

        private void Awake()
        {
            systemObject.OnSystemUpdated += OnSystemObjectRecompiled;
            totalSteps = 0;
        }

        private void OnDestroy()
        {
            systemObject.OnSystemUpdated -= OnSystemObjectRecompiled;
        }

        private void OnSystemObjectRecompiled()
        {
            lastState = null;
            totalSteps = 0;
            lastUpdateChanged = true;
            systemState = new DefaultLSystemState(systemObject.axiom, systemObject.seed);
            ExtractParameters();
            OnSystemStateUpdated?.Invoke();
        }

        public void ResetState()
        {
            lastState = null;
            totalSteps = 0;
            lastUpdateChanged = true;
            systemState = new DefaultLSystemState(systemObject.axiom, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            OnSystemStateUpdated?.Invoke();
        }

        private void ExtractParameters()
        {
            parameterNameToIndex = new Dictionary<string, int>();
            systemParameters = new double[systemObject.defaultGlobalRuntimeParameters.Count];
            for (int i = 0; i < systemObject.defaultGlobalRuntimeParameters.Count; i++)
            {
                var globalParam = systemObject.defaultGlobalRuntimeParameters[i];
                systemParameters[i] = globalParam.defaultValue;
                parameterNameToIndex[globalParam.name] = i;
            }
        }


        /// <summary>
        /// step the Lsystem forward one tick
        /// </summary>
        /// <returns>true if the state changed. false otherwise</returns>
        public void StepSystem()
        {
            try
            {
                systemObject.compiledSystem?.StepSystem(systemState, systemParameters);
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
            lastUpdateTime = Time.time;
        }
    }
}