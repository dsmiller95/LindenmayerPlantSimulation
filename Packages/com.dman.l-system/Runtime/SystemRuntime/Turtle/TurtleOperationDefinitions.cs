using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.LSystem.UnityObjects;
using Dman.LSystem.UnityObjects.StemTrunk;
using Dman.LSystem.UnityObjects.VolumetricResource;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [StructLayout(LayoutKind.Explicit)]
    public struct TurtleOperation
    {
        [FieldOffset(0)] public TurtleOperationType operationType;
        /// <summary>
        /// organ addition operation
        /// </summary>
        [FieldOffset(1)] public TurtleStemPlacementOperation stemOperation;
        /// <summary>
        /// organ addition operation
        /// </summary>
        [FieldOffset(1)] public TurtleOrganOperation meshOperation;
        /// <summary>
        /// ECS entity spawning operation
        /// </summary>
        [FieldOffset(1)] public TurtleInstantiateEntityOperator instantiateOperator;
        /// <summary>
        /// bend towards operations
        /// </summary>
        [FieldOffset(1)] public TurtleBendTowardsOperation bendTowardsOperation;
        [FieldOffset(1)] public TurtleOrientTowardsOperation orientTowardsOperation;
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

        /// <summary>
        /// rotate operation
        /// </summary>
        [FieldOffset(1)] public TurtleDiffuseVolumetricResource volumetricDiffusionOperation;

        public void Operate(
            ref TurtleState currentState,
            int indexInString,
            SymbolString<float> sourceString,
            NativeArray<TurtleOrganTemplate.Blittable> allOrgans,
            NativeList<TurtleOrganInstance> targetOrganInstances,
            NativeList<TurtleStemInstance> targetStemInstances,
            TurtleVolumetricHandles volumetricHandles,
            EntityCommandBuffer spawningEntityBuffer)
        {
            switch (operationType)
            {
                case TurtleOperationType.BEND_TOWARDS:
                    bendTowardsOperation.Operate(ref currentState, indexInString, sourceString);
                    break;
                case TurtleOperationType.ORIENT_TOWARDS:
                    orientTowardsOperation.Operate(ref currentState, indexInString, sourceString);
                    break;
                case TurtleOperationType.ADD_ORGAN:
                    meshOperation.Operate(ref currentState, indexInString, sourceString, allOrgans, targetOrganInstances, volumetricHandles);
                    break;
                case TurtleOperationType.ADD_STEM:
                    stemOperation.Operate(ref currentState, indexInString, sourceString, targetStemInstances);
                    break;
                case TurtleOperationType.INSTANTIATE_ENTITY: // TODO: get the local to world transform from somewhere other than the volumetric handles
                    instantiateOperator.Operate(ref currentState, indexInString, sourceString, spawningEntityBuffer, volumetricHandles.durabilityWriter.localToWorldTransformation);
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
                case TurtleOperationType.VOLUMETRIC_RESOURCE:
                    volumetricDiffusionOperation.Operate(ref currentState, indexInString, sourceString, volumetricHandles);
                    break;
                default:
                    break;
            }
            return;
        }
    }

    public struct TurtleVolumetricHandles
    {
        public bool IsCreated;
        public DoubleBufferNativeWritableHandle durabilityWriter;
        public CommandBufferNativeWritableHandle universalWriter;
        public VoxelWorldVolumetricLayerData.ReadOnly volumetricData;
    }

    public enum TurtleOperationType : byte
    {
        BEND_TOWARDS,
        ORIENT_TOWARDS,
        ADD_ORGAN,
        ADD_STEM,
        INSTANTIATE_ENTITY,
        ROTATE,
        SCALE_TRANSFORM,
        SCALE_THICCNESS,
        VOLUMETRIC_RESOURCE
    }
}
