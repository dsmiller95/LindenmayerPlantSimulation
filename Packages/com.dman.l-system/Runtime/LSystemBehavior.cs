using Dman.LSystem.SystemRuntime;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public class LSystemBehavior : MonoBehaviour
    {
        public LSystemObject systemObject;

        public SymbolString<double> CurrentState => currentState;
        public bool systemValid => currentSystem != null;

        private LSystem<double> currentSystem;
        private SymbolString<double> currentState;

        private double[] systemParameters;
        private Dictionary<string, int> parameterNameToIndex;

        private void Awake()
        {
            currentSystem = systemObject.Compile();
            currentState = new SymbolString<double>(systemObject.axiom);
            ExtractParameters();
        }

        public void Recompile()
        {
            currentSystem = systemObject.Compile(Random.Range(int.MinValue, int.MaxValue));
            currentState = new SymbolString<double>(systemObject.axiom);
            ExtractParameters();
        }

        public void ResetState()
        {
            lastState = "";
            currentSystem?.RestartSystem(systemObject.seed);
            currentState = new SymbolString<double>(systemObject.axiom);
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

        private string lastState;

        /// <summary>
        /// step the Lsystem forward one tick
        /// </summary>
        /// <returns>true if the state changed. false otherwise</returns>
        public bool StepSystem()
        {
            try
            {
                currentState = currentSystem?.StepSystem(currentState, systemParameters);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            var currentStringState = currentState?.ToString();
            Debug.Log(currentStringState);

            bool changed = currentStringState != lastState;
            lastState = currentStringState;
            return changed;
        }
    }
}
