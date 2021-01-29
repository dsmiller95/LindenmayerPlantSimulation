using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    public class LSystemBehavior: MonoBehaviour
    {
        public LSystemObject systemObject;

        private LSystem<double> currentSystem;

        public SymbolString<double> currentState => currentSystem?.currentSymbols;
        public bool systemValid => currentSystem != null;

        private void Awake()
        {
            currentSystem = systemObject.Compile();
        }

        public void Reset()
        {
            currentSystem = systemObject.Compile(Random.Range(int.MinValue, int.MaxValue));
        }

        public void StepSystem()
        {
            currentSystem?.StepSystem();
            Debug.Log(currentState?.symbols.ToStringFromChars());
        }
    }
}
