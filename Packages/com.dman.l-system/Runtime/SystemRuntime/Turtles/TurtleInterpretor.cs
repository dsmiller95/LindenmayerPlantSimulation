using Dman.LSystem.SystemRuntime;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;
using Dman.MeshDraftExtensions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Unity.Entities;
using System;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleInterpretor: IDisposable
    {
        private IDictionary<int, TurtleOperation> operationsByKey;
        private NativeArray<TurtleEntityPrototypeOrganTemplate> allOrganData;


        public int submeshIndexIncrementChar = '`';
        public int branchStartChar = '[';
        public int branchEndChar = ']';

        private TurtleState defaultState;

        public TurtleInterpretor(TurtleOperationSet[] operationSets, TurtleState defaultState)
        {
            var meshData = new NativeArrayBuilder<TurtleEntityPrototypeOrganTemplate>(operationSets.Sum(x => x.TotalOrganSpaceNeeded));
            operationsByKey = operationSets
                .SelectMany(x => x.GetOperators(meshData))
                .ToDictionary(x => x.Key, x => x.Value);
            allOrganData = meshData.data;
            this.defaultState = defaultState;
        }

        public void CompileStringToTransformsWithMeshIds(
            SymbolString<float> symbols,
            EntityCommandBuffer spawnCommandBuffer,
            Entity parentEntity)
        {
            var currentState = defaultState;
            var stateStack = new Stack<TurtleState>();

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
                    operation.Operate(
                        ref currentState,
                        symbolIndex,
                        symbols,
                        allOrganData,
                        spawnCommandBuffer,
                        parentEntity);
                }
            }
        }

        private bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed) return;
            allOrganData.Dispose();
            isDisposed = true;
        }
    }
}
