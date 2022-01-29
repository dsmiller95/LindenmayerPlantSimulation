using Unity.Entities;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct FlowerComponent : IComponentData
    {
        public bool hasInstantiated;
        public float rotationSpeed;
    }
}