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
        public OrganDiffusionDirection diffusionDirection;
        public VolumetricResourceLayer resourceLayer;
    }

    public enum OrganDiffusionDirection
    {
        TWO_WAY,
        CONSUME,
        PRODUCE
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
                            resourceLayerId = interactable.resourceLayer.voxelLayerId + 1
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

        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString,
            VolumetricWorldNativeWritableHandle volumetricNativeWriter,
            VoxelWorldVolumetricLayerData volumetricDataArray)
        {
            var paramIndex = sourceString.parameters[indexInString];
            if (diffusionDirection == OrganDiffusionDirection.PRODUCE && paramIndex.length == 1)
            {
                var producedAmount = sourceString.parameters[paramIndex, 0];
                var pointInLocalSpace = state.transformation.MultiplyPoint(Vector3.zero);

                volumetricNativeWriter.AppentAmountChangeToLayer(pointInLocalSpace, producedAmount, resourceLayerId);
            }
        }
    }

}
