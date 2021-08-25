using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.UnityObjects.VolumetricResource
{
    [System.Serializable]
    public class VolumetricResource
    {
        public char indicatorCharacter;
        public float diffusionConstant;
        public OrganDiffusionDirection diffusionDirection;
        public VolumetricResourceLayer resourceLayer;
    }

    /// <summary>
    /// pump out will take the entire first parameter of the symbol, and dump it into the diffusion world. it is recommended to use command
    ///     symbols for this, which only exist for one step of the l-system
    /// diffuse two-way will perform a weighted diffusion between the l-system parameter and the voxel it collides with
    /// diffuse in only is like two-way, but will only allow diffusion into the l-system
    /// diffuse out only is like two-way, but will only allow diffusion out of the l-system
    /// </summary>
    public enum OrganDiffusionDirection
    {
        DIFFUSE_TWO_WAY,
        DIFFUSE_IN_ONLY,
        DIFFUSE_OUT_ONLY,
        PUMP_OUT
    }

    [CreateAssetMenu(fileName = "TurtleVolumetricResourceDiffusion", menuName = "LSystem/TurtleVolumetricResourceDiffusion")]
    public class TurtleVolumetricResourceDiffusionOperationSet : TurtleOperationSet
    {
        public VolumetricResource[] volumetricInteractors;
        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            foreach (var interactable in volumetricInteractors)
            {
                writer.operators.Add(new TurtleOperationWithCharacter
                {
                    characterInRootFile = interactable.indicatorCharacter,
                    operation = new TurtleOperation
                    {
                        operationType = TurtleOperationType.VOLUMETRIC_RESOURCE,
                        volumetricDiffusionOperation = new TurtleDiffuseVolumetricResource
                        {
                            diffusionDirection = interactable.diffusionDirection,
                            resourceLayerId = interactable.resourceLayer.voxelLayerId,
                            diffusionConstant = interactable.diffusionConstant
                        }
                    }
                });
            }
        }
    }
    public struct TurtleDiffuseVolumetricResource
    {
        public OrganDiffusionDirection diffusionDirection;
        public int resourceLayerId;
        public float diffusionConstant;

        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString,
            VolumetricWorldNativeWritableHandle volumetricNativeWriter,
            VoxelWorldVolumetricLayerData volumetricDataArray)
        {
            var paramIndex = sourceString.parameters[indexInString];
            if(paramIndex.length != 1)
            {
                return;
            }
            var pointInLocalSpace = state.transformation.MultiplyPoint(Vector3.zero);
            var voxelIndex = volumetricNativeWriter.GetVoxelIndexFromLocalSpace(pointInLocalSpace);
            if (!voxelIndex.IsValid)
            {
                return;
            }

            var amountInSymbol = sourceString.parameters[paramIndex, 0];
            if (diffusionDirection == OrganDiffusionDirection.PUMP_OUT)
            {
                volumetricNativeWriter.AppentAmountChangeToLayer(voxelIndex, amountInSymbol, resourceLayerId);
                return;
            }
            var amountInVoxel = volumetricDataArray[voxelIndex, resourceLayerId];
            var diffusionToLSystem = (amountInVoxel - amountInSymbol) * diffusionConstant;

            switch (diffusionDirection)
            {
                case OrganDiffusionDirection.DIFFUSE_IN_ONLY:
                    diffusionToLSystem = math.max(diffusionToLSystem, 0);
                    break;
                case OrganDiffusionDirection.DIFFUSE_OUT_ONLY:
                    diffusionToLSystem = math.min(diffusionToLSystem, 0);
                    break;
                default:
                    break;
            }

            if(diffusionToLSystem == 0)
            {
                return;
            }
            sourceString.parameters[paramIndex, 0] = amountInSymbol + diffusionToLSystem;
            volumetricNativeWriter.AppentAmountChangeToLayer(voxelIndex, -diffusionToLSystem, resourceLayerId);
        }
    }

}
