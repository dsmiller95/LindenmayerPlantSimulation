using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleInterpretor : IDisposable
    {
        private DependencyTracker<NativeTurtleData> nativeDataTracker;
        public Material[] submeshMaterials;

        public int submeshIndexIncrementChar = '`';
        public int branchStartChar = '[';
        public int branchEndChar = ']';

        private TurtleState defaultState;

        public TurtleInterpretor(TurtleOperationSet[] operationSets, TurtleState defaultState)
        {
            foreach (var operationSet in operationSets)
            {
                operationSet.InternalCacheOperations();
            }

            var totalRequirements = operationSets.Select(x => x.DataReqs).Aggregate(new TurtleDataRequirements(), (agg, req) => agg + req);
            var nativeData = new NativeTurtleData(totalRequirements);
            var nativeWriter = new TurtleNativeDataWriter();

            foreach (var operationSet in operationSets)
            {
                operationSet.WriteIntoNativeData(nativeData, nativeWriter);
            }

            this.submeshMaterials = nativeWriter.materialsInOrder.ToArray();

            nativeData.operationsByKey = new NativeHashMap<int, TurtleOperation>(nativeWriter.operators.Count(), Allocator.Persistent);
            foreach (var ops in nativeWriter.operators)
            {
                nativeData.operationsByKey[ops.Key] = ops.Value;
            }

            nativeDataTracker = new DependencyTracker<NativeTurtleData>(nativeData);
            this.defaultState = defaultState;
        }

        public ICompletable<TurtleCompletionResult> CompileStringToTransformsWithMeshIds(
            DependencyTracker<SymbolString<float>> symbols,
            Mesh targetMesh)
        {
            return new TurtleStringReadingCompletable(
                targetMesh,
                submeshMaterials.Length,
                symbols,
                nativeDataTracker,
                submeshIndexIncrementChar,
                branchStartChar,
                branchEndChar,
                defaultState
                );
        }

        public void Dispose()
        {
            nativeDataTracker.Dispose();
        }

    }
}
