using Dman.LSystem.SystemRuntime;
using System;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LSystemBehavior))]
    public class TurtleInterpreterBehavior : MonoBehaviour
    {
        public TurtleOperationSet<TurtleState>[] operationSets;
        public Vector3 initialScale = Vector3.one;
        public char meshIndexIncrementor = '`';

        private TurtleInterpretor<TurtleState> turtle;
        private LSystemBehavior System => GetComponent<LSystemBehavior>();

        private void Awake()
        {
            var operatorDictionary = operationSets.SelectMany(x => x.GetOperators()).ToDictionary(x => (int)x.TargetSymbol);

            turtle = new TurtleInterpretor<TurtleState>(
                operatorDictionary,
                new TurtleState
                {
                    transformation = Matrix4x4.Scale(initialScale)
                });
            turtle.meshIndexIncrementChar = meshIndexIncrementor;

            System.OnSystemStateUpdated += OnSystemStateUpdated;
        }

        private void OnDestroy()
        {
            System.OnSystemStateUpdated -= OnSystemStateUpdated;
        }

        private void OnSystemStateUpdated()
        {
            this.InterpretSymbols(System.CurrentState);
        }

        public void InterpretSymbols(SymbolString<double> symbols)
        {
            var meshfilter = GetComponent<MeshFilter>();
            var targetMesh = meshfilter.mesh;

            // Ref is unecessary in the backing API here, which is why we're not re-assigning back from it here
            turtle.CompileStringToMesh(symbols, ref targetMesh);
        }
    }
}
