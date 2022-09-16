using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public struct NativeTurtleData : INativeDisposable
    {
        /// <summary>
        /// index all operations by their associated symbol
        /// </summary>
        public NativeHashMap<int, TurtleOperation> operationsByKey;
        public NativeArray<TurtleOrganTemplate.Blittable> allOrganData;
        public NativeArray<NativeVertexDatum> vertexData;
        public NativeArray<int> triangleData;
        public NativeArray<TurtleStemClass> stemClasses;

        private NativeArray<bool> _hasEntitySpawning;
        public bool HasEntitySpawning
        {
            get => _hasEntitySpawning[0];
            set => _hasEntitySpawning[0] = value;
        }
        public NativeTurtleData(
            TurtleDataRequirements memReqs)
        {
            operationsByKey = default;
            stemClasses = default;
            allOrganData = new NativeArray<TurtleOrganTemplate.Blittable>(memReqs.organTemplateSize, Allocator.Persistent);
            vertexData = new NativeArray<NativeVertexDatum>(memReqs.vertextDataSize, Allocator.Persistent);
            triangleData = new NativeArray<int>(memReqs.triangleDataSize, Allocator.Persistent);
            _hasEntitySpawning = new NativeArray<bool>(memReqs.triangleDataSize, Allocator.Persistent);
            HasEntitySpawning = false;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    operationsByKey.Dispose(inputDeps),
                    stemClasses.Dispose(inputDeps),
                    allOrganData.Dispose(inputDeps)
                ),
                JobHandle.CombineDependencies(
                    vertexData.Dispose(inputDeps),
                    triangleData.Dispose(inputDeps),
                    _hasEntitySpawning.Dispose(inputDeps)
                ));
        }

        public void Dispose()
        {
            _hasEntitySpawning.Dispose();
            operationsByKey.Dispose();
            stemClasses.Dispose();
            allOrganData.Dispose();
            vertexData.Dispose();
            triangleData.Dispose();
        }
    }


    public struct TurtleDataRequirements
    {
        public int vertextDataSize;
        public int triangleDataSize;
        public int organTemplateSize;

        public static TurtleDataRequirements operator +(TurtleDataRequirements a, TurtleDataRequirements b)
        {
            return new TurtleDataRequirements
            {
                vertextDataSize = a.vertextDataSize + b.vertextDataSize,
                triangleDataSize = a.triangleDataSize + b.triangleDataSize,
                organTemplateSize = a.organTemplateSize + b.organTemplateSize,
            };
        }
    }

    public class TurtleNativeDataWriter
    {
        public int indexInVertexes = 0;
        public int indexInTriangles = 0;
        public int indexInOrganTemplates = 0;
        public List<TurtleOperationWithCharacter> operators = new List<TurtleOperationWithCharacter>();
        public List<Material> materialsInOrder = new List<Material>();
        public List<TurtleStemClass> stemClasses = new List<TurtleStemClass>();

        /// <summary>
        /// gets an index for this material. will add it to existing material list if not already present
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public int GetMaterialIndex(Material mat)
        {
            var existingMaterialIndex = materialsInOrder.IndexOf(mat);
            if (existingMaterialIndex >= 0) return existingMaterialIndex;
            materialsInOrder.Add(mat);
            return materialsInOrder.Count - 1;
        }
        public int GetStemClassIndex(TurtleStemClass stemClass)
        {
            var existingIndex = stemClasses.IndexOf(stemClass);
            if (existingIndex >= 0) return existingIndex;
            stemClasses.Add(stemClass);
            return stemClasses.Count - 1;
        }
    }

    public class TurtleOperationWithCharacter
    {
        public char characterInRootFile;
        public TurtleOperation operation;
    }

    public interface ITurtleNativeDataWritable
    {
        public TurtleDataRequirements DataReqs { get; }
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
