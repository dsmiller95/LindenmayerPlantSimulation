using Dman.LSystem.SystemRuntime.NativeCollections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.PerformanceTesting;

public class UniqueSummationJobTest
{
    private void TestCountByDistinct(IEnumerable<uint> uniqueIdGenerator, int dataSize)
    {
        UnityEngine.Profiling.Profiler.BeginSample("bit counting");
        var data = new NativeArray<uint>(dataSize, Allocator.Persistent);

        var uniqueEntries = new Dictionary<uint, uint>();
        var idGenerator = uniqueIdGenerator.GetEnumerator();

        for (int i = 0; i < data.Length; i++)
        {
            idGenerator.MoveNext();
            var idHere = idGenerator.Current;
            if (!uniqueEntries.TryGetValue(idHere, out var cnt))
            {
                cnt = 0;
            }
            uniqueEntries[idHere] = cnt + 1;
            data[i] = idHere;
        }

        var counter = new CountByDistinct(data, Allocator.Persistent);

        var idCounts = counter.GetCounts();
        var dependency = counter.Schedule();
        dependency.Complete();

        data.Dispose();
        UnityEngine.Profiling.Profiler.EndSample();

        using (idCounts)
        {
            foreach (var kvp in uniqueEntries)
            {
                if (!idCounts.TryGetValue(kvp.Key, out var countResult))
                {
                    Assert.Fail($"Missing count for id: {kvp.Key}");
                }
                else
                {
                    Assert.AreEqual(kvp.Value, countResult, $"Expected count for Id '{kvp.Key}' to be {kvp.Value} but was {countResult}");
                }
            }
        }
    }

    private IEnumerable<uint> RunSequenceGenerator(double runEndProbability, uint minInt, uint maxInt)
    {
        var rand = new Random();
        uint runId = (uint)rand.Next((int)minInt, (int)maxInt);

        while (true)
        {
            if (rand.NextDouble() < runEndProbability)
            {
                runId = (uint)rand.Next((int)minInt, (int)maxInt);
            }
            yield return runId;
        }
    }

    [Test]
    public void CountsAllUniqueBitsAccuratelyWhenManyShortRuns()
    {
        TestCountByDistinct(RunSequenceGenerator(0.99, 0, 200), 100);
    }

    [Test]
    public void CountsAllUniqueBitsAccuratelyWhenHugeDataManyRuns()
    {
        TestCountByDistinct(RunSequenceGenerator(0.1, 0, 200), 10000);
    }

    [Test]
    public void CountsAllUniqueBitsAccuratelyWhenFewUniqueIds()
    {
        TestCountByDistinct(RunSequenceGenerator(0.6, 0, 5), 100);
    }

    [Test, Performance]
    public void PerformanceTestCountingBitsNoRuns()
    {
        var data = new NativeArray<uint>(1024 * 1024, Allocator.Persistent);

        var uniqueEntries = new Dictionary<uint, uint>();
        var idGenerator = RunSequenceGenerator(1, 0, 200).GetEnumerator();

        for (int i = 0; i < data.Length; i++)
        {
            idGenerator.MoveNext();
            var idHere = idGenerator.Current;
            if (!uniqueEntries.TryGetValue(idHere, out var cnt))
            {
                cnt = 0;
            }
            uniqueEntries[idHere] = cnt + 1;
            data[i] = idHere;
        }
        using (data)
        {
            NativeHashMap<uint, uint> idCounts = default;
            Measure.Method(() =>
            {
                var counter = new CountByDistinct(data, Allocator.Persistent);
                idCounts = counter.GetCounts();
                counter.Schedule().Complete();

            })
                .CleanUp(() =>
                {
                    idCounts.Dispose();
                })
                .WarmupCount(10)
                .MeasurementCount(10)
                .IterationsPerMeasurement(3)
                .GC()
                .Run();
        }
    }
    [Test, Performance]
    public void PerformanceTestCountingBits()
    {
        var data = new NativeArray<uint>(1024 * 1024, Allocator.Persistent);

        var uniqueEntries = new Dictionary<uint, uint>();
        var idGenerator = RunSequenceGenerator(0.1, 0, 200).GetEnumerator();

        for (int i = 0; i < data.Length; i++)
        {
            idGenerator.MoveNext();
            var idHere = idGenerator.Current;
            if (!uniqueEntries.TryGetValue(idHere, out var cnt))
            {
                cnt = 0;
            }
            uniqueEntries[idHere] = cnt + 1;
            data[i] = idHere;
        }
        using (data)
        {
            NativeHashMap<uint, uint> idCounts = default;
            Measure.Method(() =>
            {
                var counter = new CountByDistinct(data, Allocator.Persistent);
                idCounts = counter.GetCounts();
                counter.Schedule().Complete();

            })
                .CleanUp(() =>
                {
                    idCounts.Dispose();
                })
                .WarmupCount(10)
                .MeasurementCount(10)
                .IterationsPerMeasurement(3)
                .GC()
                .Run();
        }
    }
    [Test, Performance]
    public void PerformanceTestCountingBitsLongerRuns()
    {
        var data = new NativeArray<uint>(1024 * 1024, Allocator.Persistent);

        var uniqueEntries = new Dictionary<uint, uint>();
        var idGenerator = RunSequenceGenerator(0.01, 0, 200).GetEnumerator();

        for (int i = 0; i < data.Length; i++)
        {
            idGenerator.MoveNext();
            var idHere = idGenerator.Current;
            if (!uniqueEntries.TryGetValue(idHere, out var cnt))
            {
                cnt = 0;
            }
            uniqueEntries[idHere] = cnt + 1;
            data[i] = idHere;
        }
        using (data)
        {
            NativeHashMap<uint, uint> idCounts = default;
            Measure.Method(() =>
            {
                var counter = new CountByDistinct(data, Allocator.Persistent);
                idCounts = counter.GetCounts();
                counter.Schedule().Complete();

            })
                .CleanUp(() =>
                {
                    idCounts.Dispose();
                })
                .WarmupCount(10)
                .MeasurementCount(10)
                .IterationsPerMeasurement(3)
                .GC()
                .Run();
        }
    }
    [Test, Performance]
    public void PerformanceTestCountingBitsLongestRuns()
    {
        var data = new NativeArray<uint>(1024 * 1024, Allocator.Persistent);

        var uniqueEntries = new Dictionary<uint, uint>();
        var idGenerator = RunSequenceGenerator(0.0001, 0, 200).GetEnumerator();

        for (int i = 0; i < data.Length; i++)
        {
            idGenerator.MoveNext();
            var idHere = idGenerator.Current;
            if (!uniqueEntries.TryGetValue(idHere, out var cnt))
            {
                cnt = 0;
            }
            uniqueEntries[idHere] = cnt + 1;
            data[i] = idHere;
        }
        using (data)
        {
            NativeHashMap<uint, uint> idCounts = default;
            Measure.Method(() =>
            {
                var counter = new CountByDistinct(data, Allocator.Persistent);
                idCounts = counter.GetCounts();
                counter.Schedule().Complete();

            })
                .CleanUp(() =>
                {
                    idCounts.Dispose();
                })
                .WarmupCount(10)
                .MeasurementCount(10)
                .IterationsPerMeasurement(3)
                .GC()
                .Run();
        }
    }
}
