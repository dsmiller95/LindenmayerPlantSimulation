using Dman.LSystem.SystemRuntime.Turtle;
using Dman.Utilities.Math;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects.StemTrunk
{
    [BurstCompile]
    internal struct TurtleStemPreprocessZipupJob : IJob
    {
        public NativeArray<TurtleStemGenerationData> generationData;

        [ReadOnly]
        public NativeArray<TurtleStemInstance> stemInstances;
        [ReadOnly]
        public NativeArray<TurtleStemPreprocessParallelData> parallelData;

        public void Execute()
        {
            for (int i = 0; i < stemInstances.Length; i++)
            {
                generationData[i] = GetGenerationData(i);
            }
        }

        public TurtleStemGenerationData GetGenerationData(int stemIndex)
        {
            var instance = stemInstances[stemIndex];
            var instanceData = parallelData[stemIndex];

            if (Hint.Unlikely(instance.parentIndex < 0))
            {
                return new TurtleStemGenerationData
                {
                    uvDepth = 0,
                    normalizedVertexAngleOffset = 0
                };
            }
            
            var parentGenData = generationData[instance.parentIndex];

            return new TurtleStemGenerationData
            {
                uvDepth = parentGenData.uvDepth + instanceData.uvLength,
                normalizedVertexAngleOffset = parentGenData.normalizedVertexAngleOffset + instanceData.normalizedTriangleIndexOffset, // just add, the rotation must loop
            };
        }
    }
}
