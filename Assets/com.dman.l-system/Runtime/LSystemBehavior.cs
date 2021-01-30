using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    public class LSystemBehavior: MonoBehaviour
    {
        public LSystemObject systemObject;

        public SymbolString<double> currentState => currentSystem?.currentSymbols;
        public bool systemValid => currentSystem != null;

        private LSystem<double> currentSystem;

        private double[] systemParameters;
        private Dictionary<string, int> parameterNameToIndex;  

        private void Awake()
        {
            currentSystem = systemObject.Compile();
            this.ExtractParameters();
        }

        public void Reset()
        {
            currentSystem = systemObject.Compile(Random.Range(int.MinValue, int.MaxValue));
            this.ExtractParameters();
        }

        private void ExtractParameters()
        {
            parameterNameToIndex = new Dictionary<string, int>();
            systemParameters = new double[systemObject.defaultGlobalParameters.Length];
            for (int i = 0; i < systemObject.defaultGlobalParameters.Length; i++)
            {
                var globalParam = systemObject.defaultGlobalParameters[i];
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
                currentSystem?.StepSystem(systemParameters);
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
