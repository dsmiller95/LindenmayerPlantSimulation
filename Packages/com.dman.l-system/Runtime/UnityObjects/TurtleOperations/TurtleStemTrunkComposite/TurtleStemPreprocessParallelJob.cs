using Dman.LSystem.SystemRuntime.Turtle;
using Dman.Utilities.Math;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.UnityObjects.StemTrunk
{
    /// <summary>
    /// runs directly after the turtle compilation job, to wrapup data
    ///    needed to propigate through to the stem builder job
    /// should cache all results which will be useful in the actual stem mesh building
    /// </summary>
    [BurstCompile]
    internal struct TurtleStemPreprocessParallelJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<TurtleStemPreprocessParallelData> parallelData;

        [ReadOnly]
        public NativeArray<TurtleStemInstance> stemInstances;

        public void Execute(int stemIndex)
        {
            parallelData[stemIndex] = GetGenerationData(stemIndex);
        }

        public TurtleStemPreprocessParallelData GetGenerationData(int stemIndex)
        {
            var instance = stemInstances[stemIndex];

            if(Hint.Unlikely(instance.parentIndex < 0))
            {
                return new TurtleStemPreprocessParallelData
                {
                    uvLength = 0,
                    normalizedTriangleIndexOffset = 0
                };
            }

            var currentTransform = instance.orientation;
            var parentTransform = stemInstances[instance.parentIndex].orientation;


            var midpoint = new float3(0.5f, 0, 0);
            var circumference = currentTransform.MultiplyVector(new float3(0, 1, 0)).magnitude * 2 * math.PI;
            var distanceFromParent = (currentTransform.MultiplyPoint3x4(midpoint) - parentTransform.MultiplyPoint3x4(midpoint)).magnitude;
            var uvLength = distanceFromParent * (1f / circumference);
            var normalizedOffset = GetNormalizedCircleOffset(parentTransform, currentTransform) + 1;

            return new TurtleStemPreprocessParallelData
            {
                uvLength = uvLength,
                normalizedTriangleIndexOffset = normalizedOffset
            };
        }

        /// <summary>
        /// returns a value representing the rotation required to align the y axis of <paramref name="next"/> up as closely as possible to the y axis of <paramref name="parent"/>
        /// </summary>
        /// <param name="parent">the previous-stem orientation</param>
        /// <param name="next">the next-stem orientation</param>
        /// <returns>a value between -0.5 and 0.5, representing rotations about the x-axis from -180 to 180 degrees</returns>
        private static float GetNormalizedCircleOffset(Matrix4x4 parent, Matrix4x4 next)
        {
            var parentBasisPlaneX = parent.MultiplyVector(new Vector3(0, 0, 1));
            var parentBasisPlaneY = parent.MultiplyVector(new Vector3(0, 1, 0));
            var parentBasisPlaneNormal = parent.MultiplyVector(new Vector3(1, 0, 0));

            var nextY = next.MultiplyVector(new Vector3(0, 1, 0));
            var nextYProjectedOnParentBasisPlane = ProjectOntoPlane(nextY, parentBasisPlaneX, parentBasisPlaneY);

            var angleOffset = Vector3.SignedAngle(parentBasisPlaneY, nextYProjectedOnParentBasisPlane, parentBasisPlaneNormal);
            return angleOffset / 360f;
        }

        private static Vector3 ProjectOntoPlane(Vector3 projectionVector, Vector3 planeBasisX, Vector3 planeBasisY)
        {
            var projectedX = planeBasisX * (Vector3.Dot(planeBasisX, projectionVector));
            var projectedY = planeBasisY * (Vector3.Dot(planeBasisY, projectionVector));
            return projectedX + projectedY;
        }
    }
}
