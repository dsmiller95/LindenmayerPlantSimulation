using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Burst;
using Unity.Collections;
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
        /// <summary>
        /// floating point identity. a uint value of 0, or a color of RGBA(0,0,0,0) indicates there is no organ identity
        /// </summary>
        public UIntFloatColor32 organIdentity;
    }

    /// <summary>
    /// Used to track how big a submesh will have to be, while also tracking the current index in each array
    ///     to be used while writing to the mesh data array
    /// </summary>
    public struct TurtleMeshAllocationCounter
    {
        public int indexInVertexes;
        public int totalVertexes;

        public int indexInTriangles;
        public int totalTriangleIndexes;
    }

    public class TurtleStringReadingCompletable : ICompletable<TurtleCompletionResult>
    {
#if UNITY_EDITOR
        public string TaskDescription => "Turtle string reading completable";
#endif
        public JobHandle currentJobHandle { get; private set; }

        private NativeList<TurtleOrganInstance> organInstances;
        private DependencyTracker<NativeTurtleData> nativeData;

        private NativeArray<TurtleMeshAllocationCounter> newMeshSizeBySubmesh;
        private Mesh targetMesh;
        public TurtleStringReadingCompletable(
            Mesh targetMesh,
            int totalSubmeshes,
            DependencyTracker<SymbolString<float>> symbols,
            DependencyTracker<NativeTurtleData> nativeData,
            int submeshIndexIncrementChar,
            int branchStartChar,
            int branchEndChar,
            TurtleState defaultState,
            CustomRuleSymbols customSymbols)
        {
            this.targetMesh = targetMesh;
            this.nativeData = nativeData;

            UnityEngine.Profiling.Profiler.BeginSample("turtling job");

            var tmpHelperStack = new TmpNativeStack<TurtleState>(50, Allocator.TempJob);
            organInstances = new NativeList<TurtleOrganInstance>(100, Allocator.TempJob);
            newMeshSizeBySubmesh = new NativeArray<TurtleMeshAllocationCounter>(totalSubmeshes, Allocator.TempJob);
            var turtleCompileJob = new TurtleCompilationJob
            {
                symbols = symbols.Data,
                operationsByKey = nativeData.Data.operationsByKey,
                organData = nativeData.Data.allOrganData,

                organInstances = organInstances,
                newMeshSizeBySubmesh = newMeshSizeBySubmesh,

                nativeTurtleStack = tmpHelperStack,

                submeshIndexIncrementChar = submeshIndexIncrementChar,
                branchStartChar = branchStartChar,
                branchEndChar = branchEndChar,

                currentState = defaultState,

                customRules = customSymbols
            };

            currentJobHandle = turtleCompileJob.Schedule();

            nativeData.RegisterDependencyOnData(currentJobHandle);
            symbols.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = tmpHelperStack.Dispose(currentJobHandle);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();
            return new TurtleOrganSpawningCompletable(
                targetMesh,
                newMeshSizeBySubmesh,
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
            public NativeArray<TurtleMeshAllocationCounter> newMeshSizeBySubmesh;

            // tmp Working memory
            public TmpNativeStack<TurtleState> nativeTurtleStack;

            public CustomRuleSymbols customRules;

            public int submeshIndexIncrementChar;
            public int branchStartChar;
            public int branchEndChar;

            public TurtleState currentState;


            public void Execute()
            {
                for (int symbolIndex = 0; symbolIndex < symbols.Length; symbolIndex++)
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
                    if (customRules.hasIdentifiers && customRules.identifier == symbol)
                    {
                        currentState.organIdentity = new UIntFloatColor32(symbols.parameters[symbolIndex, 0]);
                        continue;
                    }
                    if (operationsByKey.TryGetValue(symbol, out var operation))
                    {
                        operation.Operate(
                            ref currentState,
                            newMeshSizeBySubmesh,
                            symbolIndex,
                            symbols,
                            organData,
                            organInstances);
                    }
                }
                var totalVertexes = 0;
                var totalIndexes = 0;
                for (int i = 0; i < newMeshSizeBySubmesh.Length; i++)
                {
                    var meshSize = newMeshSizeBySubmesh[i];
                    meshSize.indexInVertexes = totalVertexes;
                    meshSize.indexInTriangles = totalIndexes;
                    totalVertexes += meshSize.totalVertexes;
                    totalIndexes += meshSize.totalTriangleIndexes;
                    newMeshSizeBySubmesh[i] = meshSize;
                }
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            currentJobHandle.Complete();
            return JobHandle.CombineDependencies(
                organInstances.Dispose(inputDeps),
                newMeshSizeBySubmesh.Dispose(inputDeps)
                );
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            organInstances.Dispose();
            newMeshSizeBySubmesh.Dispose();
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
