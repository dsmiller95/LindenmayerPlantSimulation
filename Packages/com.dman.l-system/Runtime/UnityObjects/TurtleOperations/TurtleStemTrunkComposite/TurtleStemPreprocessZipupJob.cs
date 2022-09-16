using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Dman.LSystem.UnityObjects.StemTrunk
{
    [BurstCompile]
    internal struct TurtleStemPreprocessZipupJob : IJob
    {
        public NativeArray<TurtleStemGenerationData> generationData;

        [ReadOnly]
        public NativeArray<TurtleStemInstance> stemInstances;
        [ReadOnly]
        public NativeArray<TurtleStemClass> stemClasses;
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
                    normalizedVertexAngleOffset = 0,
                    uvPositiveRun = true
                };
            }

            var stemClass = stemClasses[instance.stemClassIndex];
            var parentGenData = generationData[instance.parentIndex];

            var nextGenData = new TurtleStemGenerationData
            {
                uvDepth = parentGenData.uvDepth + instanceData.uvLength,
                normalizedVertexAngleOffset = parentGenData.normalizedVertexAngleOffset + instanceData.normalizedTriangleIndexOffset, // just add, the rotation must loop
                uvPositiveRun = parentGenData.uvPositiveRun
            };

            if (stemClass.constrainUvs)
            {
                var maxUvHeight = stemClass.MaxUvYHeight();

                var spaceUp = maxUvHeight - parentGenData.uvDepth;
                var spaceDown = parentGenData.uvDepth;

                if (parentGenData.uvPositiveRun)
                {
                    if (spaceUp >= instanceData.uvLength || spaceUp > spaceDown)
                    {
                        nextGenData.uvDepth = math.clamp(parentGenData.uvDepth + instanceData.uvLength, 0, maxUvHeight);
                        nextGenData.uvPositiveRun = true;
                    }
                    else
                    {
                        nextGenData.uvDepth = math.clamp(parentGenData.uvDepth - instanceData.uvLength, 0, maxUvHeight);
                        nextGenData.uvPositiveRun = false;
                    }
                }
                else
                {
                    if (spaceDown >= instanceData.uvLength || spaceDown > spaceUp)
                    {
                        nextGenData.uvDepth = math.clamp(parentGenData.uvDepth - instanceData.uvLength, 0, maxUvHeight);
                        nextGenData.uvPositiveRun = false;
                    }
                    else
                    {
                        nextGenData.uvDepth = math.clamp(parentGenData.uvDepth + instanceData.uvLength, 0, maxUvHeight);
                        nextGenData.uvPositiveRun = true;
                    }
                }
            }
            return nextGenData;
        }
    }
}
