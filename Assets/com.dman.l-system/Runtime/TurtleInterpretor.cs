using Dman.LSystem.SystemRuntime;
using Dman.MeshDraftExtensions;
using ProceduralToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem
{
    public class TurtleInterpretor
    {
        //private IDictionary<int, MeshDraft> draftsByKey;
        ///// <summary>
        ///// keys to use to apply transforms to the turtle's position
        /////     if a key is also contained in the drafts dictionary, the transformation is
        /////     applied after the draft is placed.
        ///// </summary>
        //private IDictionary<int, Matrix4x4> transformationByKey;

        private IDictionary<int, ITurtleOperator> operationsByKey;

        public int meshIndexIncrementChar = '`';
        public int branchStartChar = '[';
        public int branchEndChar = ']';
        private Matrix4x4 rootTransform;

        public TurtleInterpretor(IDictionary<int, ITurtleOperator> operations) : this(operations, Matrix4x4.identity) { }

        public TurtleInterpretor(IDictionary<int, ITurtleOperator> operations, Matrix4x4 rootTransform)
        {
            operationsByKey = operations;
            this.rootTransform = rootTransform;
        }

        public Mesh CompileStringToMesh(SymbolString<double> symbols)
        {
            var resultMeshes = new List<MeshDraft>();
            resultMeshes.Add(new MeshDraft());

            var currentState = new TurtleState
            {
                transformation = rootTransform,
                submeshIndex = 0
            };

            var stateStack = new Stack<TurtleState>();

            for (int symbolIndex = 0; symbolIndex < symbols.symbols.Length; symbolIndex++)
            {
                var symbol = symbols.symbols[symbolIndex];
                if(symbol == branchStartChar)
                {
                    stateStack.Push(currentState);
                    continue;
                }
                if(symbol == branchEndChar)
                {
                    currentState = stateStack.Pop();
                    continue;
                }
                if(symbol == meshIndexIncrementChar)
                {
                    currentState.submeshIndex++;
                    if (resultMeshes.Count < currentState.submeshIndex + 1)
                        resultMeshes.Add(new MeshDraft());
                    continue;
                }
                if(operationsByKey.TryGetValue(symbol, out var operation))
                {
                    currentState = operation.Operate(
                        currentState,
                        symbols.parameters[symbolIndex],
                        resultMeshes[currentState.submeshIndex]);
                }
                //if(draftsByKey.TryGetValue(symbol, out var newDraft))
                //{
                //    resultMeshes[currentState.submeshIndex].AddWithTransform(newDraft, currentState.transformation);
                //}
                //if (transformationByKey.TryGetValue(symbol, out var transform))
                //{
                //    currentState.transformation *= transform;
                //}
            }

            var resultMeshbulder = new CompoundMeshDraft();
            foreach (var meshOutput in resultMeshes)
            {
                resultMeshbulder.Add(meshOutput);
            }
            return resultMeshbulder.ToMeshWithSubMeshes();
        }
    }
}
