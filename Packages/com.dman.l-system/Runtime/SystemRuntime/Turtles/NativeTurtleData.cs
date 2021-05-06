using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct NativeTurtleData : INativeDisposable
    {
        public NativeHashMap<int, TurtleOperation> operationsByKey;
        public NativeArray<TurtleOrganTemplate.Blittable> allOrganData;
        //public NativeArray<int> vertexData;

        public NativeTurtleData(
            TurtleDataRequirements memReqs)
        {
            this.operationsByKey = default;
            allOrganData = new NativeArray<TurtleOrganTemplate.Blittable>(memReqs.organTemplateSize, Allocator.Persistent);
            //vertexData = new NativeArray<int>(memReqs.vertextDataSize, Allocator.Persistent);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                operationsByKey.Dispose(inputDeps),
                allOrganData.Dispose(inputDeps));
        }

        public void Dispose()
        {
            operationsByKey.Dispose();
            allOrganData.Dispose();
            //vertexData.Dispose();
        }
    }


    public struct TurtleDataRequirements
    {
        public int vertextDataSize;
        public int organTemplateSize;

        public static TurtleDataRequirements operator +(TurtleDataRequirements a, TurtleDataRequirements b)
        {
            return new TurtleDataRequirements
            {
                vertextDataSize = a.vertextDataSize + b.vertextDataSize,
                organTemplateSize = a.organTemplateSize + b.organTemplateSize,
            };
        }
    }

    public class TurtleNativeDataWriter
    {
        public int indexInVertexes = 0;
        public int indexInOrganTemplates = 0;
        public List<KeyValuePair<int, TurtleOperation>> operators = new List<KeyValuePair<int, TurtleOperation>>();
    }

    public interface ITurtleNativeDataWritable
    {
        public TurtleDataRequirements DataReqs {get;}
        public void WriteIntoNativeData(
            NativeTurtleData nativeData,
            TurtleNativeDataWriter writer);
    }

    public struct TurtleCompletionResult
    {

    }

    public static class TurlteDataRequirementsExtensions
    {
        public static TurtleDataRequirements Sum(this IEnumerable<TurtleDataRequirements> self)
        {
            return self.Aggregate(new TurtleDataRequirements(), (agg, next) => agg + next);
        }
    }
}
