using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct NativeTurtleData: INativeDisposable
    {
        public NativeHashMap<int, TurtleOperation> operationsByKey;
        public NativeArray<TurtleEntityPrototypeOrganTemplate> allOrganData;

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                operationsByKey.Dispose(inputDeps),
                allOrganData.Dispose(inputDeps));
        }

        public void Dispose()
        {
            operationsByKey.Dispose();
            allOrganData.Dispose();
        }
    }

    public struct TurtleCompletionResult
    {

    }

    public class TurtleInterpretor : IDisposable
    {
        private DependencyTracker<NativeTurtleData> nativeData;

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
            var operationsByKey = new NativeHashMap<int, TurtleOperation>(allOps.Count(), Allocator.Persistent);
            foreach (var ops in allOps)
            {
                operationsByKey[ops.Key] = ops.Value;
            }
            nativeData = new DependencyTracker<NativeTurtleData>(
                new NativeTurtleData
                {
                    operationsByKey = operationsByKey,
                    allOrganData = meshData.data
                });
            this.defaultState = defaultState;
        }

        public ICompletable<TurtleCompletionResult> CompileStringToTransformsWithMeshIds(
            DependencyTracker<SymbolString<float>> symbols,
            EntityCommandBufferSystem spawnCommandBuffer,
            Entity parentEntity)
        {
            return new TurtleStringReadingCompletable(
                symbols,
                spawnCommandBuffer,
                parentEntity,
                nativeData,
                submeshIndexIncrementChar,
                branchStartChar,
                branchEndChar,
                defaultState
                );
        }

        public void Dispose()
        {
            nativeData.Dispose();
        }

    }
}
