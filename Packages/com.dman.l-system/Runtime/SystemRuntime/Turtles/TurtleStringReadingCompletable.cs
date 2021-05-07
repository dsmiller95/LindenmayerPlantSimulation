using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct TurtleOrganInstance
    {
        public ushort organIndexInAllOrgans;
        public float4x4 organTransform;
        public JaggedIndexing vertexMemorySpace;
        public JaggedIndexing trianglesMemorySpace;
    }

    public struct TurtleMeshAllocationCounter
    {
        public int totalVertexes;
        public int totalTriangleIndexes;
    }

    public class TurtleStringReadingCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }

        private NativeList<TurtleOrganInstance> organInstances;
        private DependencyTracker<NativeTurtleData> nativeData;

        private NativeArray<TurtleMeshAllocationCounter> newMeshSize;
        private Mesh targetMesh;
        public TurtleStringReadingCompletable(
            Mesh targetMesh,
            DependencyTracker<SymbolString<float>> symbols,
            DependencyTracker<NativeTurtleData> nativeData,
            int submeshIndexIncrementChar,
            int branchStartChar,
            int branchEndChar,
            TurtleState defaultState)
        {
            this.targetMesh = targetMesh;
            this.nativeData = nativeData;

            var tmpHelperStack = new TmpNativeStack<TurtleState>(50, Allocator.TempJob);
            organInstances = new NativeList<TurtleOrganInstance>(100, Allocator.TempJob);
            newMeshSize = new NativeArray<TurtleMeshAllocationCounter>(1, Allocator.TempJob);
            var turtleCompileJob = new TurtleCompilationJob
            {
                symbols = symbols.Data,
                operationsByKey = nativeData.Data.operationsByKey,
                organData = nativeData.Data.allOrganData,

                organInstances = organInstances,
                newMeshSize = newMeshSize,

                nativeTurtleStack = tmpHelperStack,

                submeshIndexIncrementChar = submeshIndexIncrementChar,
                branchStartChar = branchStartChar,
                branchEndChar = branchEndChar,

                currentState = defaultState
            };

            currentJobHandle = turtleCompileJob.Schedule();

            nativeData.RegisterDependencyOnData(currentJobHandle);
            symbols.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = tmpHelperStack.Dispose(currentJobHandle);
        }

        public ICompletable<TurtleCompletionResult> StepNext()
        {
            currentJobHandle.Complete();
            var meshSize = newMeshSize[0];
            newMeshSize.Dispose();
            return new TurtleOrganSpawningCompletable(
                targetMesh,
                meshSize,
                nativeData,
                organInstances
                );
        }

        [BurstCompile]
        public struct TurtleCompilationJob : IJob
        {
            // Inputs
            [ReadOnly]
            public SymbolString<float> symbols;
            [ReadOnly]
            public NativeHashMap<int, TurtleOperation> operationsByKey;
            [ReadOnly]
            public NativeArray<TurtleOrganTemplate.Blittable> organData;

            // Outputs
            public NativeList<TurtleOrganInstance> organInstances;
            public NativeArray<TurtleMeshAllocationCounter> newMeshSize;

            // tmp Working memory
            public TmpNativeStack<TurtleState> nativeTurtleStack;

            public int submeshIndexIncrementChar;
            public int branchStartChar;
            public int branchEndChar;

            public TurtleState currentState;


            public void Execute()
            {
                var meshCount = new TurtleMeshAllocationCounter();
                for (int symbolIndex = 0; symbolIndex < symbols.symbols.Length; symbolIndex++)
                {
                    var symbol = symbols.symbols[symbolIndex];
                    if (symbol == branchStartChar)
                    {
                        nativeTurtleStack.Push(currentState);
                        continue;
                    }
                    if (symbol == branchEndChar)
                    {
                        currentState = nativeTurtleStack.Pop();
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
                            ref meshCount,
                            symbolIndex,
                            symbols,
                            organData,
                            organInstances);
                    }
                }
                newMeshSize[0] = meshCount;
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            currentJobHandle.Complete();
            return JobHandle.CombineDependencies(
                organInstances.Dispose(inputDeps),
                newMeshSize.Dispose(inputDeps)
                );
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            organInstances.Dispose();
            newMeshSize.Dispose();
        }

        public TurtleCompletionResult GetData()
        {
            return default;
        }

        public string GetError()
        {
            return null;
        }

        public bool HasErrored()
        {
            return false;
        }

        public bool IsComplete()
        {
            return false;
        }
    }
}
