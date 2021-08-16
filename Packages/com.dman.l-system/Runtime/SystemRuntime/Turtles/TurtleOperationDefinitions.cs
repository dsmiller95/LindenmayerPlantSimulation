using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.UnityObjects;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [StructLayout(LayoutKind.Explicit)]
    public struct TurtleOperation
    {
        [FieldOffset(0)] public TurtleOperationType operationType;
        /// <summary>
        /// organ addition operation
        /// </summary>
        [FieldOffset(1)] public TurtleMeshOperation meshOperation;
        /// <summary>
        /// bend towards operations
        /// </summary>
        [FieldOffset(1)] public TurtleBendTowardsOperation bendTowardsOperation;
        /// <summary>
        /// thickness operation
        /// </summary>
        [FieldOffset(1)] public TurtleThiccnessOperation thiccnessOperation;
        /// <summary>
        /// scale operation
        /// </summary>
        [FieldOffset(1)] public TurtleScaleOperation scaleOperation;

        /// <summary>
        /// rotate operation
        /// </summary>
        [FieldOffset(1)] public TurtleRotationOperation rotationOperation;

        public void Operate(
            ref TurtleState currentState,
            NativeArray<TurtleMeshAllocationCounter> meshSizeCounterPerSubmesh,
            int indexInString,
            SymbolString<float> sourceString,
            NativeArray<TurtleOrganTemplate.Blittable> allOrgans,
            NativeList<TurtleOrganInstance> targetOrganInstances,
            VolumetricWorldNativeWritableHandle volumetricNativeWriter)
        {
            switch (operationType)
            {
                case TurtleOperationType.BEND_TOWARDS:
                    bendTowardsOperation.Operate(ref currentState, indexInString, sourceString);
                    break;
                case TurtleOperationType.ADD_ORGAN:
                    meshOperation.Operate(ref currentState, meshSizeCounterPerSubmesh, indexInString, sourceString, allOrgans, targetOrganInstances, volumetricNativeWriter);
                    break;
                case TurtleOperationType.ROTATE:
                    rotationOperation.Operate(ref currentState, indexInString, sourceString);
                    break;
                case TurtleOperationType.SCALE_TRANSFORM:
                    scaleOperation.Operate(ref currentState, indexInString, sourceString);
                    break;
                case TurtleOperationType.SCALE_THICCNESS:
                    thiccnessOperation.Operate(ref currentState, indexInString, sourceString);
                    break;
                default:
                    break;
            }
            return;
        }
    }

    public enum TurtleOperationType : byte
    {
        BEND_TOWARDS,
        ADD_ORGAN,
        ROTATE,
        SCALE_TRANSFORM,
        SCALE_THICCNESS
    }
}
