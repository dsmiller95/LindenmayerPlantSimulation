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
    public class NativeDelayedReadable<T> :
        IDisposable,
        INativeDisposable
        where T : IDisposable, INativeDisposable 
    {
        public T openReadData;
        public T data;

        public JobHandle dataWriterDependencies { get; private set; } = default;
        public JobHandle dataReaderDependencies { get; private set; } = default;

        public NativeDelayedReadable(T openReadData, T writerData)
        {
            data = writerData;
            this.openReadData = openReadData;
        }

        public void CompleteAllDependencies()
        {
            dataWriterDependencies.Complete();
            dataReaderDependencies.Complete();
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
