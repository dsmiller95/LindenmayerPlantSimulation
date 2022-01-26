using Unity.Entities;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class TurtleSpawnData : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<TurtleSpawnedParameters>(entity);
        }
    }


    [InternalBufferCapacity(8)]
    public struct TurtleSpawnedParameters : IBufferElementData
    {
        public float parameterValue;
    }
}
