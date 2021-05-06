using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleStringReadingCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }


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
            var buffer = spawnCommandBuffer.CreateCommandBuffer();

            var tmpHelperStack = new TmpNativeStack<TurtleState>(50, Allocator.TempJob);
            var turtleCompileJob = new TurtleCompilationJob
            {
                symbols = symbols.Data,
                operationsByKey = nativeData.Data.operationsByKey,
                organData = nativeData.Data.allOrganData,
                nativeTurtleStack = tmpHelperStack,

                submeshIndexIncrementChar = submeshIndexIncrementChar,
                branchStartChar = branchStartChar,
                branchEndChar = branchEndChar,

                organSpawnBuffer = buffer,
                parentEntity = parentEntity,
                currentState = defaultState
            };

            currentJobHandle = turtleCompileJob.Schedule();
            spawnCommandBuffer.AddJobHandleForProducer(currentJobHandle);

            nativeData.RegisterDependencyOnData(currentJobHandle);
            symbols.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = tmpHelperStack.Dispose(currentJobHandle);
        }

        public ICompletable<TurtleCompletionResult> StepNext()
        {
            currentJobHandle.Complete();
            return new CompleteCompletable<TurtleCompletionResult>(new TurtleCompletionResult());
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return inputDeps;
        }

        public void Dispose()
        {
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



        [BurstCompile]
        public struct TurtleCompilationJob : IJob
        {
            [ReadOnly]
            public SymbolString<float> symbols;
            [ReadOnly]
            public NativeHashMap<int, TurtleOperation> operationsByKey;
            [ReadOnly]
            public NativeArray<TurtleEntityPrototypeOrganTemplate> organData;

            public TmpNativeStack<TurtleState> nativeTurtleStack;

            public int submeshIndexIncrementChar;
            public int branchStartChar;
            public int branchEndChar;
            public EntityCommandBuffer organSpawnBuffer;
            public Entity parentEntity;

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
                            organSpawnBuffer,
                            parentEntity);
                    }
                }
            }
        }
    }
}
