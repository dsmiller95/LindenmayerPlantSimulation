using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.NativeCollections
{
    /// <summary>
    /// used to count up distinct values
    /// depricated by a compute shader which can do the same work, but in the GPU
    /// </summary>
    public class CountByDistinct
    {
        private NativeArray<uint> sourceData;
        private NativeArray<bool> runFlags;
        private NativeHashMap<uint, uint> finalCounts;

        public CountByDistinct(NativeArray<uint> sourceData, Allocator allocator = Allocator.TempJob)
        {
            this.sourceData = sourceData;
            runFlags = new NativeArray<bool>(sourceData.Length, allocator, NativeArrayOptions.ClearMemory);
            finalCounts = new NativeHashMap<uint, uint>(100, allocator);
        }

        public NativeHashMap<uint, uint> GetCounts()
        {
            return finalCounts;
        }

        public JobHandle Schedule(int batchSize = 10000, JobHandle dependency = default)
        {
            var sumJob = new IdSummation()
            {
                allIds = sourceData,
                runFlags = runFlags
            };
            dependency = sumJob.ScheduleBatch(sourceData.Length, batchSize, dependency);

            var sumCollectorJob = new SummationCollector
            {
                allIds = sourceData,
                runFlags = runFlags,
                idCounts = finalCounts
            };
            dependency = sumCollectorJob.Schedule(dependency);
            // todo: better parallelization
            dependency = runFlags.Dispose(dependency);

            return dependency;
        }

        /// <summary>
        /// will iterate through its chunk of the id array and collect together groups of unique ids
        /// for any run, will replace the data in the ids array with metadata and will flag the metaFlag at that point
        ///     to be true
        /// </summary>
        [BurstCompile]
        struct IdSummation : IJobParallelForBatch
        {
            public NativeArray<uint> allIds;
            public NativeArray<bool> runFlags;

            public void Execute(int startIndex, int count)
            {
                var currentRunId = allIds[startIndex];
                var currentRunIndex = startIndex;

                for (int i = 1; i < count; i++)
                {
                    var idIndex = startIndex + i;
                    var id = allIds[idIndex];
                    //if (id == 0)
                    //{
                    //    return;
                    //}

                    if (id == currentRunId)
                    {
                        if (currentRunIndex == idIndex - 1)
                        {
                            // this is the beginning of a run and there are 2 identical elements so far
                            allIds[currentRunIndex + 1] = 2;
                            runFlags[currentRunIndex] = true;
                        }
                        else
                        {
                            // this is a continuation of a run of some length greater than 2
                            allIds[currentRunIndex + 1]++;
                        }
                    }
                    else
                    {
                        // a run has been broken (or never formed at all)
                        currentRunId = id;
                        currentRunIndex = idIndex;
                        runFlags[idIndex] = false;
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through the whole id array, looking for metaflags which indicate a consolidated run
        /// </summary>
        [BurstCompile]
        struct SummationCollector : IJob
        {
            public NativeArray<uint> allIds;
            public NativeArray<bool> runFlags;
            public NativeHashMap<uint, uint> idCounts;

            public void Execute()
            {
                var idIndex = 0;
                while (idIndex < allIds.Length)
                {
                    var flag = runFlags[idIndex];
                    var id = allIds[idIndex];
                    if (flag)
                    {
                        // run-flags indicate this index is marked with a run
                        var runLength = allIds[idIndex + 1];
                        IncrementIdCount(id, runLength);
                        idIndex += (int)runLength;
                    }
                    else
                    {
                        // if we are here, and there is no flag, it must be a single element
                        IncrementIdCount(id, 1);
                        idIndex += 1;
                    }
                }
            }

            private void IncrementIdCount(uint id, uint extraCount)
            {
                if (!idCounts.TryGetValue(id, out var count))
                {
                    count = 0;
                }
                idCounts[id] = count + extraCount;
            }
        }

    }
}
