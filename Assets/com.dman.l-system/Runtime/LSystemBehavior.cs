using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    public class LSystemBehavior: MonoBehaviour
    {
        public string axiom;
        public string[] rules;

        private LSystem mySystem;

        public SymbolString currentState => mySystem.currentSymbols;

        private void Awake()
        {
            mySystem = new LSystem(axiom, rules.Select(x => new BasicRule(x)));
        }

        public void StepSystem()
        {
            mySystem.StepSystem();
            Debug.Log(currentState.symbols.ToStringFromChars());
        }
    }
}
