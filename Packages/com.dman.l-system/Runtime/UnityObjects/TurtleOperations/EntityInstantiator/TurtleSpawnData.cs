using Dman.Math;
using Unity.Entities;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class TurtleSpawnData : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<TurtleSpawnedParameters>(entity);
            dstManager.AddComponent<TurtleSpawnedState>(entity);
        }
    }


    [InternalBufferCapacity(8)]
    public struct TurtleSpawnedParameters : IBufferElementData
    {
        public float parameterValue;
    }

    public struct TurtleSpawnedState : IComponentData
    {
        /// <summary>
        /// the local transformation of the organ inside the plant mesh
        /// </summary>
        public Matrix4x4 localTransform;
        /// <summary>
        /// the current thickness applied to any meshes which use thickness
        /// </summary>
        public float thickness;
        /// <summary>
        /// Custom data in the turtle state, set by the 
        /// </summary>
        public byte4 customData;
    }
}
