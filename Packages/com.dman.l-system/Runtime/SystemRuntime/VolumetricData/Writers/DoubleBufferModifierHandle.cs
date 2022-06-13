using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public class DoubleBufferModifierHandle : ModifierHandle
    {
        public NativeArray<float> valuesA;
        public NativeArray<float> valuesB;
        /// <summary>
        /// the layer index which is buffered into valuesA and valuesB
        /// </summary>
        public readonly int doubleBufferedLayerIndex;

        public JobHandle writeDependency;
        public bool newDataIsAvailable;
        public bool mostRecentDataInA;


        public NativeArray<float> newValues => mostRecentDataInA ? valuesA : valuesB;
        public NativeArray<float> oldValues => mostRecentDataInA ? valuesB : valuesA;


        private VoxelVolume volume;

        public DoubleBufferModifierHandle(VoxelVolume voxels, int doubleBufferedLayerIndex)
        {
            valuesA = new NativeArray<float>(voxels.totalVoxels, Allocator.Persistent);
            valuesB = new NativeArray<float>(voxels.totalVoxels, Allocator.Persistent);
            mostRecentDataInA = true;

            this.volume = voxels;
            this.doubleBufferedLayerIndex = doubleBufferedLayerIndex;
        }

        public override bool ConsolidateChanges(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency)
        {
            // TODO: consider skipping if writeDependency is not complete yet. should the job chain keep extending, or should
            //  consolidation be deffered?
            if (!newDataIsAvailable)
            {
                return false;
            }
            var consolidationJob = new VoxelMarkerConsolidation
            {
                allBaseMarkers = layerData,
                oldMarkerLevels = oldValues,
                newMarkerLevels = newValues,
                markerLayerIndex = doubleBufferedLayerIndex,
            };
            dependency = consolidationJob.Schedule(newValues.Length, 1000, dependency + writeDependency);
            RegisterReadDependency(dependency);
            newDataIsAvailable = false;
            return true;
        }
        public override void RemoveEffects(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency)
        {
            var layout = layerData.VoxelLayout;
            var subtractCleanupJob = new NativeArrayAdditionNegativeProtection
            {
                allBaseMarkers = layerData,
                layerToAdd = newValues,
                layerMultiplier = -1,
                markerLayerIndex = doubleBufferedLayerIndex,
                totalLayersInBase = layout.dataLayerCount
            };
            dependency = subtractCleanupJob.Schedule(layout.volume.totalVoxels, 1000, dependency + writeDependency);
        }

        public DoubleBufferNativeWritableHandle GetNextNativeWritableHandle(
            Matrix4x4 localToWorldTransform,
            ref JobHandleWrapper dependency)
        {
            if (IsDisposed)
            {
                throw new System.ObjectDisposedException("DoubleBufferModifierHandle", "Tried to write a modification via a disposed writing handle");
            }
            UnityEngine.Profiling.Profiler.BeginSample("volume clearing");
            if (!newDataIsAvailable)
            {
                // Only swap data if the old data hasn't been picked up yet.
                //  just update the new data where it is
                mostRecentDataInA = !mostRecentDataInA;
                newDataIsAvailable = true;
            }
            var mostRecentVolumeData = mostRecentDataInA ? valuesA : valuesB;
            var volumeClearJob = new NativeArrayClearJob
            {
                newValue = 0f,
                writeArray = mostRecentVolumeData
            };

            dependency = volumeClearJob.Schedule(mostRecentVolumeData.Length, 10000, dependency + writeDependency);
            writeDependency = dependency;

            UnityEngine.Profiling.Profiler.EndSample();

            return new DoubleBufferNativeWritableHandle(
                mostRecentVolumeData,
                volume,
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

        protected override JobHandle InternalDispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                valuesA.Dispose(inputDeps),
                valuesB.Dispose(inputDeps));
        }
    }
}
