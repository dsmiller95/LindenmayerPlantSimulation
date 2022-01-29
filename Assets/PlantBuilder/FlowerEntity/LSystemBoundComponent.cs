using Unity.Entities;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    [GenerateAuthoringComponent]
    public struct LSystemBoundComponent : IComponentData
    {
        public float plantId;
        public float organId;
        public float lastResourceTransferTime;
        public float resourceAmount;

        /// <summary>
        /// how long should this entity stay alive after the last update recieved from the l-system
        ///     it will be automatically destroyed when the stream of updates terminates
        /// </summary>
        [Tooltip("The lifespan of the entity after the last update recieved")]
        public float lifespanWithoutUpdate;

        public bool ShouldExpire(float currentTime)
        {
            return (lastResourceTransferTime + lifespanWithoutUpdate) < currentTime;
        }
    }
}