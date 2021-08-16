using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Mathematics;
using UnityEngine;

using Unity.Entities;
using System.Collections.Generic;

namespace Dman.LSystem.UnityObjects
{
    /// <summary>
    /// used to instantiate all game objects which could be used by the turtle entity spawning system
    /// </summary>
    public class TurtleSpawnEntitiesInstantiator : MonoBehaviour, IConvertGameObjectToEntity
    {
        public static TurtleSpawnEntitiesInstantiator instance;

        public TurtleSpawnData[] spawnableEntityPrefabs;

        private Dictionary<int, Entity> entitiesByGoInstanceId;

        public Entity GetEntityPrefab(TurtleSpawnData spawnablePrefab)
        {
            if(entitiesByGoInstanceId.TryGetValue(spawnablePrefab.gameObject.GetInstanceID(), out var entity))
            {
                return entity;
            }
            throw new System.Exception($"spawnable prefab {spawnablePrefab} not registered with the turtle spawn entities instantiator");
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            entitiesByGoInstanceId = new Dictionary<int, Entity>();
            using (BlobAssetStore assetStore = new BlobAssetStore())
            {
                foreach (var spawnableEntity in spawnableEntityPrefabs)
                {
                    var prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(spawnableEntity.gameObject,
                        GameObjectConversionSettings.FromWorld(dstManager.World, assetStore));

                    entitiesByGoInstanceId[spawnableEntity.gameObject.GetInstanceID()] = prefabEntity;
                }
            }
        }
    }
}
