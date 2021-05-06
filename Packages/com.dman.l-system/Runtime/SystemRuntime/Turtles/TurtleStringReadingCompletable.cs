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

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct TurtleOrganInstance
    {
        public ushort organIndexInAllOrgans;
        public float4x4 organTransform;
    }

    public class TurtleStringReadingCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }

        private Entity parentEntity;
        private EntityCommandBufferSystem spawnCommandBuffer;
        private NativeList<TurtleOrganInstance> organInstances;
        private DependencyTracker<NativeTurtleData> nativeData;

        public TurtleStringReadingCompletable(
            DependencyTracker<SymbolString<float>> symbols,
            EntityCommandBufferSystem spawnCommandBuffer,
            Entity parentEntity,
            DependencyTracker<NativeTurtleData> nativeData,
            int submeshIndexIncrementChar,
            int branchStartChar,
            int branchEndChar,
            TurtleState defaultState)
        {
            this.parentEntity = parentEntity;
            this.spawnCommandBuffer = spawnCommandBuffer;
            this.nativeData = nativeData;

            var tmpHelperStack = new TmpNativeStack<TurtleState>(50, Allocator.TempJob);
            organInstances = new NativeList<TurtleOrganInstance>(100, Allocator.TempJob);
            var turtleCompileJob = new TurtleCompilationJob
            {
                symbols = symbols.Data,
                operationsByKey = nativeData.Data.operationsByKey,
                organData = nativeData.Data.allOrganData,
                nativeTurtleStack = tmpHelperStack,

                submeshIndexIncrementChar = submeshIndexIncrementChar,
                branchStartChar = branchStartChar,
                branchEndChar = branchEndChar,

                organInstances = organInstances,
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
            return new TurtleOrganSpawningCompletable(
                spawnCommandBuffer,
                parentEntity,
                nativeData,
                organInstances
                );
        }

        [BurstCompile]
        public struct TurtleCompilationJob : IJob
        {
            [ReadOnly]
            public SymbolString<float> symbols;
            [ReadOnly]
            public NativeHashMap<int, TurtleOperation> operationsByKey;
            [ReadOnly]
            public NativeArray<TurtleOrganTemplate.Blittable> organData;

            public NativeList<TurtleOrganInstance> organInstances;

            public TmpNativeStack<TurtleState> nativeTurtleStack;

            public int submeshIndexIncrementChar;
            public int branchStartChar;
            public int branchEndChar;

            public TurtleState currentState;


            public void Execute()
            {
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
                            symbolIndex,
                            symbols,
                            organData,
                            organInstances);
                    }
                }
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            currentJobHandle.Complete();
            return organInstances.Dispose(inputDeps);
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            organInstances.Dispose();
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
