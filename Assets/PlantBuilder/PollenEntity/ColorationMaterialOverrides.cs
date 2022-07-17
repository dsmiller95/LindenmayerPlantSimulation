using Unity.Entities;
using UnityEngine;
using Unity.Rendering;

namespace Assets.Demo.PlantBuilder
{
    public class ColorationMaterialOverrides : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<ColorIndexOverride>(entity);
            dstManager.AddComponent<VariegationOverride>(entity);
        }
    }

    [MaterialProperty("_ColorIndex", MaterialPropertyFormat.Float)]
    public struct ColorIndexOverride : IComponentData
    {
        public float Value;
    }
    [MaterialProperty("_VariegationLevel", MaterialPropertyFormat.Float)]
    public struct VariegationOverride : IComponentData
    {
        public float Value;
    }
}