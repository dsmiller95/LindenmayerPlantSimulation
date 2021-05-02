using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Unity.Entities;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleInterpretor: IDisposable
    {
        private NativeHashMap<int, TurtleOperation> operationsByKey;
        private NativeArray<TurtleEntityPrototypeOrganTemplate> allOrganData;
        private JobHandle pendingJobs;

        public int submeshIndexIncrementChar = '`';
        public int branchStartChar = '[';
        public int branchEndChar = ']';

        private TurtleState defaultState;

        public TurtleInterpretor(TurtleOperationSet[] operationSets, TurtleState defaultState)
        {
            var meshData = new NativeArrayBuilder<TurtleEntityPrototypeOrganTemplate>(operationSets.Sum(x => x.TotalOrganSpaceNeeded));
            var allOps = operationSets
                .SelectMany(x => x.GetOperators(meshData))
                .ToList();
            operationsByKey = new NativeHashMap<int, TurtleOperation>(allOps.Count(), Allocator.Persistent);
            foreach (var ops in allOps)
            {
                operationsByKey[ops.Key] = ops.Value;
            }
            allOrganData = meshData.data;
            this.defaultState = defaultState;
        }

        public JobHandle CompileStringToTransformsWithMeshIds(
            SymbolString<float> symbols,
            EntityCommandBufferSystem spawnCommandBuffer,
            Entity parentEntity)
        {
            var buffer = spawnCommandBuffer.CreateCommandBuffer();

            var tmpHelperStack = new TmpNativeStack<TurtleState>(50, Allocator.TempJob);
            var turtleCompileJob = new TurtleCompilationJob
            {
                symbols = symbols,
                operationsByKey = operationsByKey,
                organData = allOrganData,
                nativeTurtleStack = tmpHelperStack,

                submeshIndexIncrementChar = submeshIndexIncrementChar,
                branchStartChar = branchStartChar,
                branchEndChar = branchEndChar,

                organSpawnBuffer = buffer,
                parentEntity = parentEntity,
                currentState = defaultState
            };

            var handle = turtleCompileJob.Schedule();
            spawnCommandBuffer.AddJobHandleForProducer(handle);

            handle = tmpHelperStack.Dispose(handle);

            pendingJobs = JobHandle.CombineDependencies(handle, pendingJobs);

            return handle;
        }

        private bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed) return;
            pendingJobs.Complete();
            allOrganData.Dispose();
            operationsByKey.Dispose();
            isDisposed = true;
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
