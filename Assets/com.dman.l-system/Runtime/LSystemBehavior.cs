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
            parameterNameToIndex = new Dictionary<string, int>();
            systemParameters = new double[systemObject.defaultGlobalParameters.Length];
            for (int i = 0; i < systemObject.defaultGlobalParameters.Length; i++)
            {
                var globalParam = systemObject.defaultGlobalParameters[i];
                systemParameters[i] = globalParam.defaultValue;
                parameterNameToIndex[globalParam.name] = i;
            }
        }

        public void Reset()
        {
            currentSystem = systemObject.Compile(Random.Range(int.MinValue, int.MaxValue));
        }

        public void StepSystem()
        {
            currentSystem?.StepSystem(systemParameters);
            Debug.Log(currentState?.ToString());
        }
    }
}
