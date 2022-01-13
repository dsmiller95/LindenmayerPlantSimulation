using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct PollenComponent : IComponentData
    {
        public float lifespanRemaining;
        public float totalLifespan;
    }
}