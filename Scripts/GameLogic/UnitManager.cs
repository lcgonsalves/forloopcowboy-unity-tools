using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Environment;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// Manages NPC placement and tracking.
    /// TODO: define invalidation policy
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        public enum Side
        {
            Attacker,
            Defender
        }

        public enum SpawnType
        {
            Grounded,
            Aerial
        }

        [Serializable]
        protected class SpawnPoint
        {
            public WaypointNode node;
            public SpawnType type;

            public SpawnPoint(WaypointNode node, SpawnType type)
            {
                this.node = node;
                this.type = type;
            }
        }

        [Serializable]
        public class SpawnedGameObject
        {
            public Side side;
            public IManagedGameObject managedGameObject;
            public GameObject gameObject => managedGameObject.gameObject;

            public SpawnedGameObject(Side side, GameObject obj)
            {
                this.side = side;
                this.managedGameObject = obj.Managed();
            }

            public SpawnedGameObject(Side side, IManagedGameObject obj)
            {
                this.side = side;
                this.managedGameObject = obj;
            }
        }

        [SerializeField] protected List<SpawnPoint> attackerSpawnPoints = new List<SpawnPoint>();
        [SerializeField] protected List<SpawnPoint> defenderSpawnPoints = new List<SpawnPoint>();

        protected List<SpawnedGameObject> SpawnedGameObjects = new List<SpawnedGameObject>();

        // cache
        private GameObject[] spawnedAttackerCache;
        private GameObject[] spawnedDefenderCache;

        private bool attackerCacheOutdated = true;
        private bool defenderCacheOutdated = true;

        public void AddSpawnPoint(Side side, SpawnType type, WaypointNode node)
        {
            switch (side)
            {
                case Side.Attacker:
                    attackerSpawnPoints.Add(new SpawnPoint(node, type));
                    break;
                case Side.Defender:
                    defenderSpawnPoints.Add(new SpawnPoint(node, type));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "fuck ass balls");
            }
        }

        // todo: check for end of chain to see if there's already someone there, if so can't spawn

        /// <summary>
        /// Spawns game object for given side on first available spawn point
        /// of specified type. Spawned object follows starting waypoint indefinitely.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="prefab"></param>
        /// <param name="spawnType"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Spawn(
            Side side,
            GameObject prefab,
            SpawnType spawnType = SpawnType.Grounded
        )
        {
            switch (side)
            {
                case Side.Attacker:
                    attackerCacheOutdated = true;
                    Spawn(side, prefab, spawnType, attackerSpawnPoints);
                    break;
                case Side.Defender:
                    defenderCacheOutdated = true;
                    Spawn(side, prefab, spawnType, defenderSpawnPoints);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private void Spawn(Side side, GameObject prefab, SpawnType spawnType, List<SpawnPoint> spawnPoints)
        {
            var spawnAt = spawnPoints.Find(_ => _.type == spawnType);
            if (spawnAt != null)
            {
                var instance = Instantiate(prefab, spawnAt.node.transform.position, Quaternion.identity);
                var navigation = instance.GetComponent<AdvancedNavigation>();
                var managedGameObj =
                    instance.GetOrElseAddComponent<UnitManagerGameObjectExtension.ManagedMonoBehaviour>();

                if (navigation == null)
                    throw new NullReferenceException(
                        "Spawned game objects must have an AdvancedNavigation component attached.");
                navigation.FollowWaypoint(spawnAt.node);
                SpawnedGameObjects.Add(new SpawnedGameObject(side, managedGameObj));
            }
            else throw new NullReferenceException("No spawn points found for type " + nameof(spawnType));
        }

        public void Start()
        {
            // start garbage collection:
            // coroutine that periodically checks if game objects are ready to 
            StartCoroutine(GarbageCollection());
        }

        [SerializeField] private float garbageCollectionInterval = 5f;

        private IEnumerator GarbageCollection()
        {
            var indicesToRemove = new List<int>(SpawnedGameObjects.Capacity);
            while (true)
            {
                for (int i = 0; i < SpawnedGameObjects.Count; i++)
                {
                    var spawnedGameObject = SpawnedGameObjects[i];
                    if (spawnedGameObject.managedGameObject.ShouldDestroy())
                    {
                        indicesToRemove.Add(i);
                        GameObject.Destroy(spawnedGameObject.gameObject);
                    }
                }

                foreach (var i in indicesToRemove)
                {
                    SpawnedGameObjects.RemoveAt(i);
                }

                indicesToRemove.Clear();

                yield return new WaitForSeconds(garbageCollectionInterval);
            }
        }

        /// <summary>
        /// Returns all spawned objects from a given side.
        /// This operation is cached, so no iteration is performed if no new
        /// elements have been added or expired on the selected side.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public GameObject[] GetSpawned(Side side)
        {
            RefreshCacheIfNeeded();

            switch (side)
            {
                case Side.Attacker:
                    return spawnedAttackerCache;
                case Side.Defender:
                    return spawnedDefenderCache;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private void RefreshCacheIfNeeded()
        {
            if (attackerCacheOutdated)
            {
                spawnedAttackerCache = SpawnedGameObjects.Where(_ => _.side == Side.Attacker).Select(_ => _.gameObject)
                    .ToArray();
                attackerCacheOutdated = false;
            }

            if (defenderCacheOutdated)
            {
                spawnedDefenderCache = SpawnedGameObjects.Where(_ => _.side == Side.Defender).Select(_ => _.gameObject)
                    .ToArray();
                defenderCacheOutdated = false;
            }
        }

    }

    public static class UnitManagerGameObjectExtension
    {
        /// <summary>
        /// Game objects by default are never destroyed.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool ShouldDestroy(this GameObject obj)
        {
            return false;
        }

        public static IManagedGameObject Managed(this GameObject obj)
        {
            return new ManagedGameObject(obj);
        }

        public class ManagedGameObject : IManagedGameObject
        {
            public GameObject gameObject { get; private set; }

            public bool ShouldDestroy()
            {
                return false;
            }

            public ManagedGameObject(GameObject obj)
            {
                gameObject = obj;
            }
        }

        public class ManagedMonoBehaviour : MonoBehaviour, IManagedGameObject
        {
            public new GameObject gameObject => base.gameObject;

            public virtual bool ShouldDestroy()
            {
                return false;
            }
        }
    }

    public interface IManagedGameObject
    {
        public GameObject gameObject { get; }

        public bool ShouldDestroy();
    }
}
 