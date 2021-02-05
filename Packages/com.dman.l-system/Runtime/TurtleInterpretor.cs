using Dman.LSystem.SystemRuntime;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public class TurtleInterpretor<T> where T: struct
    {
        private IDictionary<int, ITurtleOperator<T>> operationsByKey;

        public int meshIndexIncrementChar = '`';
        public int branchStartChar = '[';
        public int branchEndChar = ']';
        
        private T defaultState;

        public TurtleInterpretor(IDictionary<int, ITurtleOperator<T>> operations, T defaultState)
        {
            operationsByKey = operations;
            this.defaultState = defaultState;
        }

        public void CompileStringToMesh(SymbolString<double> symbols, ref Mesh targetMesh)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Turtle interpretation");
            var resultMeshes = new List<MeshDraft>();
            resultMeshes.Add(new MeshDraft());

            var currentState = new TurtleMeshState<T>(defaultState);

            var stateStack = new Stack<TurtleMeshState<T>>();

            for (int symbolIndex = 0; symbolIndex < symbols.symbols.Length; symbolIndex++)
            {
                var symbol = symbols.symbols[symbolIndex];
                if (symbol == branchStartChar)
                {
                    stateStack.Push(currentState);
                    continue;
                }
                if (symbol == branchEndChar)
                {
                    currentState = stateStack.Pop();
                    continue;
                }
                if (symbol == meshIndexIncrementChar)
                {
                    currentState.submeshIndex++;
                    if (resultMeshes.Count < currentState.submeshIndex + 1)
                        resultMeshes.Add(new MeshDraft());
                    continue;
                }
                if (operationsByKey.TryGetValue(symbol, out var operation))
                {
                    currentState.turtleBaseState = operation.Operate(
                        currentState.turtleBaseState,
                        symbols.parameters[symbolIndex],
                        resultMeshes[currentState.submeshIndex]);
                }
            }

            UnityEngine.Profiling.Profiler.BeginSample("Mesh construction");
            var resultMeshbulder = new CompoundMeshDraft();
            foreach (var meshOutput in resultMeshes)
            {
                resultMeshbulder.Add(meshOutput);
            }
            resultMeshbulder.ToMeshWithSubMeshes(ref targetMesh);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
