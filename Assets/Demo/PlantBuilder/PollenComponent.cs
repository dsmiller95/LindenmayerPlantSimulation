using Unity.Entities;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct PollenComponent : IComponentData
    {
        public float lifespanRemaining;
        public float totalLifespan;
        public float scaleMultiplier;
    }
}