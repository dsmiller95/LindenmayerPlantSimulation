using Dman.LSystem.SystemRuntime.Turtle;
using Dman.Utilities.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects.StemTrunk
{
    [BurstCompile]
    internal struct TurtleStemPreprocessJob : IJob
    {
        public NativeArray<TurtleStemGenerationData> generationData;

        [ReadOnly]
        public NativeArray<TurtleStemInstance> stemInstances;

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

            if(instance.parentIndex < 0)
            {
                return new TurtleStemGenerationData
                {
                    uvDepth = 0
                };
            }

            var currentTransform = instance.orientation;
            var circumference = currentTransform.MultiplyVector(new float3(0, 1, 0)).magnitude * 2 * math.PI;

            var parentGenData = generationData[instance.parentIndex];
            var parentTransform = stemInstances[instance.parentIndex].orientation;
            var distanceFromParent = (currentTransform.MultiplyPoint3x4(float3.zero) - parentTransform.MultiplyPoint3x4(float3.zero)).magnitude;

            var uvLength = distanceFromParent * (1f / circumference);

            return new TurtleStemGenerationData
            {
                uvDepth = parentGenData.uvDepth + uvLength,
            };
        }
    }
}
