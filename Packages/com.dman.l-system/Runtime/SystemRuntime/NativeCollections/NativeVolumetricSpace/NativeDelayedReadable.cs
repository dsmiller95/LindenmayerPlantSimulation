using Dman.LSystem.SystemRuntime.VolumetricData;
using System;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace
{
    public class NativeDelayedReadable :
        IDisposable,
        INativeDisposable
    {
        public NativeArray<float> openReadData;
        public NativeArray<float> data;

        public JobHandle dataWriterDependencies { get; private set; } = default;
        public JobHandle dataReaderDependencies { get; private set; } = default;

        public bool IsCreated => data.IsCreated && openReadData.IsCreated;

        private Allocator allocatorUsed;
        public NativeDelayedReadable(int dataSize, Allocator allocator)
        {
            allocatorUsed = allocator;
            data = new NativeArray<float>(dataSize, allocator, NativeArrayOptions.ClearMemory);
            openReadData = new NativeArray<float>(dataSize, allocator, NativeArrayOptions.ClearMemory);
        }

        public void CompleteAndForceCopy()
        {
            dataWriterDependencies.Complete();
            dataReaderDependencies.Complete();

            openReadData.CopyFrom(data);
        }

        public void RegisterWritingDependency(JobHandle writer)
        {
            this.dataWriterDependencies = JobHandle.CombineDependencies(writer, dataWriterDependencies);
        }

        public void RegisterReadingDependency(JobHandle reader)
        {
            this.dataReaderDependencies = JobHandle.CombineDependencies(reader, dataReaderDependencies);
        }

        public void Dispose()
        {
            dataReaderDependencies.Complete();
            dataWriterDependencies.Complete();
            data.Dispose();
            openReadData.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                data.Dispose(inputDeps),
                openReadData.Dispose(inputDeps)
                );
        }
    }
}
