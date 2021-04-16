using Dman.LSystem.SystemRuntime;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;
using Dman.MeshDraftExtensions;

namespace Dman.LSystem
{
    public class TurtleInterpretor<T> where T : struct
    {
        private IDictionary<int, ITurtleOperator<T>> operationsByKey;

        public int submeshIndexIncrementChar = '`';
        public int branchStartChar = '[';
        public int branchEndChar = ']';

        private T defaultState;

        public TurtleInterpretor(IDictionary<int, ITurtleOperator<T>> operations, T defaultState)
        {
            operationsByKey = operations;
            this.defaultState = defaultState;
        }

        /// <summary>
        /// Compile the given symbols into a mesh, using the operations defined in this turtle instance
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="targetMesh"></param>
        public List<Material> CompileStringToMesh(SymbolString<float> symbols, ref Mesh targetMesh)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Turtle interpretation");
            var resultMeshes = new List<MeshDraft>();
            var targetMaterials = new List<Material>();

            //var meshInstances = this.CompileStringToTransformsWithMeshIds(symbols);
            //foreach (var meshInstance in meshInstances.GetTurtleMeshInstances())
            //{
            //    var meshTemplate = meshInstances.GetMeshTemplate(meshInstance.meshIndex);
            //    var templateInstance = meshTemplate.GetOrganTemplateValue();
            //    var meshMaterialIndex = targetMaterials.IndexOf(templateInstance.material);
            //    if(meshMaterialIndex == -1)
            //    {
            //        targetMaterials.Add(templateInstance.material);
            //        resultMeshes.Add(new MeshDraft());
            //        meshMaterialIndex = targetMaterials.Count - 1;
            //    }
            //    resultMeshes[meshMaterialIndex].AddWithTransform(templateInstance.draft, meshInstance.transformation);
            //}


            UnityEngine.Profiling.Profiler.BeginSample("Mesh construction");
            var resultMeshbulder = new CompoundMeshDraft();
            foreach (var meshOutput in resultMeshes)
            {
                resultMeshbulder.Add(meshOutput);
            }
            resultMeshbulder.ToMeshWithSubMeshes(ref targetMesh);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();
            return targetMaterials;
        }

        public TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate> CompileStringToTransformsWithMeshIds(SymbolString<float> symbols)
        {
            var meshInstanceTracker = new TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate>();
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
                if (symbol == submeshIndexIncrementChar)
                {
                    currentState.submeshIndex++;
                    continue;
                }
                if (operationsByKey.TryGetValue(symbol, out var operation))
                {
                    currentState.turtleBaseState = operation.Operate(
                        currentState.turtleBaseState,
                        symbols.parameters[symbolIndex],
                        meshInstanceTracker);
                }
            }

            return meshInstanceTracker;
        }
    }
}
