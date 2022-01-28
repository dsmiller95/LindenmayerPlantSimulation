using Unity.Entities;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct LSystemBoundComponent : IComponentData
    {
        public float plantId;
        public float organId;
        public float lastResourceTransferTime;
        public float resourceAmount;
    }
}