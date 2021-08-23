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

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public class VolumetricWorldModifierHandle
    {
        public NativeArray<float> durabilityA;
        public NativeArray<float> durabilityB;
        public NativeList<LayerModificationCommand> modificationCommands;

        public JobHandle writeDependency;
        public bool newDataIsAvailable;
        public bool mostRecentDataInA;
        public bool isDisposed;


        public NativeArray<float> newDurability => mostRecentDataInA ? durabilityA : durabilityB;
        public NativeArray<float> oldDurability => mostRecentDataInA ? durabilityB : durabilityA;


        public VolumetricWorldVoxelLayout voxelLayout;

        public OrganVolumetricWorld volumetricWorld;

        public VolumetricWorldModifierHandle(VolumetricWorldVoxelLayout voxels, OrganVolumetricWorld volumetricWorld)
        {
            durabilityA = new NativeArray<float>(voxels.totalVoxels, Allocator.Persistent);
            durabilityB = new NativeArray<float>(voxels.totalVoxels, Allocator.Persistent);
            modificationCommands = new NativeList<LayerModificationCommand>(10, Allocator.Persistent);
            mostRecentDataInA = true;

            this.voxelLayout = voxels;
            this.volumetricWorld = volumetricWorld;
        }

        public VolumetricWorldNativeWritableHandle GenerateWritableHandleAndSwitchLatestData(
            Matrix4x4 localToWorldTransform, 
            ref JobHandle dependency)
        {
            UnityEngine.Profiling.Profiler.BeginSample("volume clearing");
            if (!newDataIsAvailable)
            {
                // Only swap data if the old data hasn't been picked up yet.
                //  just update the new data where it is
                mostRecentDataInA = !mostRecentDataInA;
                newDataIsAvailable = true;
            }
            var mostRecentVolumeData = mostRecentDataInA ? durabilityA : durabilityB;
            var volumeClearJob = new NativeArrayClearJob
            {
                newValue = 0f,
                writeArray = mostRecentVolumeData
            };
            modificationCommands.Clear();

            dependency = JobHandle.CombineDependencies(dependency, writeDependency);
            dependency = volumeClearJob.Schedule(mostRecentVolumeData.Length, 10000, dependency);
            writeDependency = dependency;

            UnityEngine.Profiling.Profiler.EndSample();

            return new VolumetricWorldNativeWritableHandle(
                mostRecentVolumeData,
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

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (isDisposed)
            {
                return inputDeps;
            }
            isDisposed = true;
            return JobHandle.CombineDependencies(
                durabilityA.Dispose(inputDeps),
                durabilityB.Dispose(inputDeps),
                modificationCommands.Dispose(inputDeps));
        }
    }
}
