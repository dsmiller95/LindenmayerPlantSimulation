using Dman.LSystem.SystemRuntime;
using System;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TurtleInterpreterBehavior : MonoBehaviour
    {
        public TurtleOperationSet<TurtleState>[] operationSets;
        public Vector3 initialScale = Vector3.one;
        public char meshIndexIncrementor = '`';

        private TurtleInterpretor<TurtleState> turtle;

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
        }

        public void InterpretSymbols(SymbolString<double> symbols)
        {
            var output = turtle.CompileStringToMesh(symbols);
            GetComponent<MeshFilter>().mesh = output;
        }
    }
}
