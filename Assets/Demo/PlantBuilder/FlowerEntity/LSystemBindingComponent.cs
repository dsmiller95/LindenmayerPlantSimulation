using Unity.Entities;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct LSystemBindingComponent : IComponentData
    {
        public Entity boundPrefab;
    }
}