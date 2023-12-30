using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dman.LSystem.SystemRuntime.NativeJobsUtilities;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleInterpretor : OrganPositioningTurtleInterpretor
    {
        private OrganVolumetricWorld volumetricWorld;
        private DoubleBufferModifierHandle durabilityWriterHandle;
        private CommandBufferModifierHandle commandBufferWriter;

        private VoxelCapReachedTimestampEffect damageCapFlags;


        public TurtleInterpretor(
            List<TurtleOperationSet> operationSets,
            TurtleState defaultState,
            ISymbolRemapper symbolRemapper,
            CustomRuleSymbols customSymbols,
            OrganVolumetricWorld volumetricWorld,
            VoxelCapReachedTimestampEffect damageCapFlags) : base(operationSets, defaultState, symbolRemapper, customSymbols)
        {
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

        public async UniTask CompileStringToMesh(
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


            using var meshResult = await this.CompileToInstances(
                symbols,
                token,
                volumetricLocalToWorld: localToWorldTransform,
                volumeWorldReferences: volumeWorldReferences);

            await this.RenderOrganInstancesToMesh(meshResult, targetMesh, token);
        }


        public override void Dispose()
        {
            base.Dispose();
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
