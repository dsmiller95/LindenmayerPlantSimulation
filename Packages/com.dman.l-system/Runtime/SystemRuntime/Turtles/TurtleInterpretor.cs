using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleInterpretor : IDisposable
    {
        private DependencyTracker<NativeTurtleData> nativeDataTracker;
        public Material[] submeshMaterials;

        private TurtleState defaultState;
        private CustomRuleSymbols customSymbols;


        private OrganVolumetricWorld volumetricWorld;
        private DoubleBufferModifierHandle durabilityWriterHandle;
        private CommandBufferModifierHandle commandBufferWriter;

        private VoxelCapReachedTimestampEffect damageCapFlags;


        public TurtleInterpretor(
            List<TurtleOperationSet> operationSets,
            TurtleState defaultState,
            LinkedFileSet linkedFiles,
            CustomRuleSymbols customSymbols,
            OrganVolumetricWorld volumetricWorld,
            VoxelCapReachedTimestampEffect damageCapFlags)
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

            this.customSymbols = customSymbols;


            nativeDataTracker = new DependencyTracker<NativeTurtleData>(nativeData);
            this.defaultState = defaultState;

            this.volumetricWorld = volumetricWorld;
            this.damageCapFlags = damageCapFlags;
            RefreshVolumetricWriters();
        }

        private void RefreshVolumetricWriters()
        {
            if (volumetricWorld == null)
            {
                durabilityWriterHandle?.Dispose();
                durabilityWriterHandle = null;
                commandBufferWriter?.Dispose();
                commandBufferWriter = null;
                return;
            }
            if (this.durabilityWriterHandle?.IsDisposed ?? true)
            {
                durabilityWriterHandle = volumetricWorld.GetDoubleBufferedWritableHandle();
            }
            if (this.commandBufferWriter?.IsDisposed ?? true)
            {
                commandBufferWriter = volumetricWorld.GetCommandBufferWritableHandle();
            }
        }

        public async UniTask CompileStringToTransformsWithMeshIds(
            DependencyTracker<SymbolString<float>> symbols,
            Mesh targetMesh,
            Matrix4x4 localToWorldTransform,
            CancellationToken token)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Turtle has been disposed and cannot be used");
            }

            RefreshVolumetricWriters();
            var volumeWorldReferences = volumetricWorld == null ? null : new TurtleVolumeWorldReferences
            {
                world = volumetricWorld,
                durabilityWriter = durabilityWriterHandle,
                universalLayerWriter = commandBufferWriter,
                damageFlags = damageCapFlags,
            };


            var meshResult = await TurtleStringReadingCompletable.ReadString(
                symbols,
                nativeDataTracker,
                defaultState,
                customSymbols,
                volumeWorldReferences,
                localToWorldTransform,
                token);

            await TurtleMeshBuildingCompletable.BuildMesh(
                targetMesh,
                submeshMaterials.Length,
                meshResult,
                nativeDataTracker,
                token);
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
            if (volumetricWorld == null)
            {
                durabilityWriterHandle?.Dispose();
                commandBufferWriter?.Dispose();
            }
            else
            {
                volumetricWorld?.DisposeWritableHandle(durabilityWriterHandle).Complete();
                volumetricWorld?.DisposeWritableHandle(commandBufferWriter).Complete();
            }
        }

    }
}
