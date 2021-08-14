using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleInterpretor : IDisposable
    {
        private DependencyTracker<NativeTurtleData> nativeDataTracker;
        public Material[] submeshMaterials;

        public int submeshIndexIncrementChar;
        public int branchStartChar;
        public int branchEndChar;

        private TurtleState defaultState;
        private CustomRuleSymbols customSymbols;

        
        private OrganVolumetricWorld volumetricWorld;
        private VolumetricWorldWritableHandle volumeWriterHandle;

        private OrganDamageWorld damageWorld;


        public TurtleInterpretor(
            List<TurtleOperationSet> operationSets,
            TurtleState defaultState,
            LinkedFileSet linkedFiles,
            CustomRuleSymbols customSymbols,
            OrganVolumetricWorld volumetricWorld,
            OrganDamageWorld damageWorld,
            char submeshIndex = '`',
            char startChar = '[', char endChar = ']')
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

            submeshMaterials = nativeWriter.materialsInOrder.ToArray();

            nativeData.operationsByKey = new NativeHashMap<int, TurtleOperation>(nativeWriter.operators.Count(), Allocator.Persistent);
            foreach (var ops in nativeWriter.operators)
            {
                var realSymbol = linkedFiles.GetSymbolFromRoot(ops.characterInRootFile);
                nativeData.operationsByKey[realSymbol] = ops.operation;
            }

            submeshIndexIncrementChar = linkedFiles.GetSymbolFromRoot(submeshIndex);
            branchStartChar = linkedFiles.GetSymbolFromRoot(startChar);
            branchEndChar = linkedFiles.GetSymbolFromRoot(endChar);
            this.customSymbols = customSymbols;


            nativeDataTracker = new DependencyTracker<NativeTurtleData>(nativeData);
            this.defaultState = defaultState;

            this.volumetricWorld = volumetricWorld;
            this.volumeWriterHandle = volumetricWorld.GetNewWritableHandle();
            this.damageWorld = damageWorld;
        }

        public ICompletable<TurtleCompletionResult> CompileStringToTransformsWithMeshIds(
            DependencyTracker<SymbolString<float>> symbols,
            Mesh targetMesh,
            Matrix4x4 localToWorldTransform)
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("Turtle has been disposed and cannot be used");
            }
            return new TurtleStringReadingCompletable(
                targetMesh,
                submeshMaterials.Length,
                symbols,
                nativeDataTracker,
                submeshIndexIncrementChar,
                branchStartChar,
                branchEndChar,
                defaultState,
                customSymbols,
                this.volumeWriterHandle,
                this.damageWorld,
                localToWorldTransform
                );
        }

        private bool IsDisposed = false;

        public void Dispose()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("Cannot dispose turtle, has already been disposed");
            }
            IsDisposed = true;
            nativeDataTracker.Dispose();
            volumetricWorld.DisposeWritableHandle(volumeWriterHandle).Complete();
        }

    }
}
