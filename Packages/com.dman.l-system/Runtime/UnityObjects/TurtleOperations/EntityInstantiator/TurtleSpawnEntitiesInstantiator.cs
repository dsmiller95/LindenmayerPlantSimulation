using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    /// <summary>
    /// used to instantiate all game objects which could be used by the turtle entity spawning system
    /// </summary>
    public class TurtleSpawnEntitiesInstantiator : MonoBehaviour
    {
        public static TurtleSpawnEntitiesInstantiator instance;

        public TurtleSpawnData[] spawnableEntityPrefabs;

        private Dictionary<int, Entity> entitiesByGoInstanceId;
        private BlobAssetStore prefabAssetStore;

        public Entity GetEntityPrefab(TurtleSpawnData spawnablePrefab)
        {
            if (entitiesByGoInstanceId.TryGetValue(spawnablePrefab.gameObject.GetInstanceID(), out var entity))
            {
                return entity;
            }
            throw new System.Exception($"spawnable prefab {spawnablePrefab} not registered with the turtle spawn entities instantiator");
        }

        private void Awake()
        {
            instance = this;
            entitiesByGoInstanceId = new Dictionary<int, Entity>();
            prefabAssetStore?.Dispose();
            prefabAssetStore = new BlobAssetStore();
            foreach (var spawnableEntity in spawnableEntityPrefabs)
            {
                var prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(spawnableEntity.gameObject,
                    GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, prefabAssetStore));

                entitiesByGoInstanceId[spawnableEntity.gameObject.GetInstanceID()] = prefabEntity;
            }
        }

        private void OnDestroy()
        {
            prefabAssetStore.Dispose();
            prefabAssetStore = null;
        }

        //public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        //{
        //    instance = this;
        //    entitiesByGoInstanceId = new Dictionary<int, Entity>();
        //    using (BlobAssetStore assetStore = new BlobAssetStore())
        //    {
        //        foreach (var spawnableEntity in spawnableEntityPrefabs)
        //        {
        //            var prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(spawnableEntity.gameObject,
        //                GameObjectConversionSettings.FromWorld(dstManager.World, assetStore));

        //            entitiesByGoInstanceId[spawnableEntity.gameObject.GetInstanceID()] = prefabEntity;
        //        }
        //    }
        //}
    }
}
