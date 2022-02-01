using Unity.Entities;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct LSystemBoundComponent : IComponentData, System.IEquatable<LSystemBoundComponent>
    {
        public float plantId;
        public float organId;
        public bool Equals(LSystemBoundComponent other)
        {
            return other.plantId == plantId && other.organId == organId;
        }

        public override bool Equals(object obj)
        {
            if (obj is LSystemBoundComponent other)
            {
                return this.Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var plantIdHash = plantId.GetHashCode();
            var plantHashRot = (plantIdHash << 16) | (plantIdHash >> 16);
            return plantHashRot ^ organId.GetHashCode();
        }
    }
}