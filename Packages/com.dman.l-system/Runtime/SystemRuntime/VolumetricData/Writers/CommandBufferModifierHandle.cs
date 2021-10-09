using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Dman.LSystem;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public class CommandBufferModifierHandle : ModifierHandle
    {
        public NativeList<LayerModificationCommand> modificationCommands;

        public JobHandle writeDependency;
        public bool newDataIsAvailable;
        public bool IsDisposed { get; private set; }

        public VolumetricWorldVoxelLayout voxelLayout;

        public CommandBufferModifierHandle(VolumetricWorldVoxelLayout voxels)
        {
            modificationCommands = new NativeList<LayerModificationCommand>(10, Allocator.Persistent);

            this.voxelLayout = voxels;
        }

        public bool ConsolidateChanges(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency)
        {
            if (!newDataIsAvailable)
            {
                return false;
            }
            var commandPlaybackJob = new LayerModificationCommandPlaybackJob
            {
                commands = modificationCommands,
                dataArray = layerData,
                voxelLayout = voxelLayout
            };
            dependency = commandPlaybackJob.Schedule(dependency + writeDependency);
            RegisterReadDependency(dependency);
            newDataIsAvailable = false;
            return true;
        }
        public void RemoveEffects(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency)
        {
            // command buffer has no record of the effects it has had on the world
            return;
        }

        public CommandBufferNativeWritableHandle GetNextNativeWritableHandle(Matrix4x4 localToWorldTransform)
        {
            if (!newDataIsAvailable)
            {
                // this is the first writable handle allocated since the last consolidation
                //  if another handle is allocated before consolidation, the change buffer should be retained
                writeDependency.Complete();
                modificationCommands.Clear();
                newDataIsAvailable = true;
            }

            return new CommandBufferNativeWritableHandle(
                modificationCommands,
                voxelLayout, 
                localToWorldTransform);
        }

        public void RegisterWriteDependency(JobHandle newWriteDependency)
        {
            this.writeDependency = JobHandle.CombineDependencies(newWriteDependency, this.writeDependency);
        }

        public void RegisterReadDependency(JobHandle newReadDependency)
        {
            this.writeDependency = JobHandle.CombineDependencies(newReadDependency, this.writeDependency);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            modificationCommands.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsDisposed)
            {
                return inputDeps;
            }
            IsDisposed = true;
            return modificationCommands.Dispose(inputDeps);
        }
    }
}
