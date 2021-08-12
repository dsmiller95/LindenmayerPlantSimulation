using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public class VolumetricWorldWritableHandle
    {
        public NativeArray<float> dataA;
        public NativeArray<float> dataB;
        public JobHandle writeDependency;
        public bool newDataIsAvailable;
        public bool mostRecentDataInA;
        public bool isDisposed;

        public NativeArray<float> newData => mostRecentDataInA ? dataA : dataB;
        public NativeArray<float> oldData => mostRecentDataInA ? dataB : dataA;


        public VolumetricWorldVoxelLayout voxelLayout;

        public VolumetricWorldWritableHandle(VolumetricWorldVoxelLayout voxels)
        {
            dataA = new NativeArray<float>(voxels.totalVolumeDataSize, Allocator.Persistent);
            dataB = new NativeArray<float>(voxels.totalVolumeDataSize, Allocator.Persistent);
            mostRecentDataInA = true;

            this.voxelLayout = voxels;
        }

        public VolumetricWorldNativeWritableHandle GenerateWritableHandleAndSwitchLatestData(Matrix4x4 localToWorldTransform, ref JobHandle dependency)
        {
            UnityEngine.Profiling.Profiler.BeginSample("volume clearing");
            mostRecentDataInA = !mostRecentDataInA;
            newDataIsAvailable = true;
            var mostRecentVolumeData = mostRecentDataInA ? dataA : dataB;
            var volumeClearJob = new NativeArrayClearJob
            {
                newValue = 0f,
                writeArray = mostRecentVolumeData
            };
            dependency = JobHandle.CombineDependencies(dependency, writeDependency);
            dependency = volumeClearJob.Schedule(mostRecentVolumeData.Length, 10000, dependency);
            writeDependency = dependency;

            UnityEngine.Profiling.Profiler.EndSample();

            return new VolumetricWorldNativeWritableHandle(mostRecentVolumeData, voxelLayout, localToWorldTransform);
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
                dataA.Dispose(inputDeps),
                dataB.Dispose(inputDeps));
        }
    }
}
