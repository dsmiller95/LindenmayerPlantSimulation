using Dman.EntityUtilities;
using Unity.Entities;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    /// <summary>
    /// used to instantiate all game objects which could be used by the turtle entity spawning system
    /// </summary>
    public class TurtleSpawnEntitiesInstantiator : MonoBehaviour
    {

        private static TurtleSpawnEntitiesInstantiator _instance;
        public static TurtleSpawnEntitiesInstantiator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<TurtleSpawnEntitiesInstantiator>();
                }
                return _instance;
            }
        }

        public EntityPrefabRegistry prefabRegistry;

        public Entity GetEntityPrefab(TurtleSpawnData spawnablePrefab)
        {
            return prefabRegistry.GetEntityPrefab(spawnablePrefab.gameObject);
        }
    }
}
