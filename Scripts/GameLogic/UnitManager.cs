using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
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

        public static Side GetOpposing(Side s) => s == Side.Attacker ? Side.Defender : Side.Attacker;

        public enum SpawnType
        {
            Grounded,
            Aerial
        }

        public string SpawnLayer = "Default";

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
            public GameObject gameObject => managedGameObject?.gameObject;
            public WaypointNode spawnedAt = null;

            public SpawnedGameObject(Side side, GameObject obj)
            {
                this.side = side;
                this.managedGameObject = obj.GetManaged();
            }

            public SpawnedGameObject(Side side, IManagedGameObject obj)
            {
                this.side = side;
                this.managedGameObject = obj;
            }

            public SpawnedGameObject(Side side, IManagedGameObject obj, WaypointNode spawnedAt) : this(side, obj)
            {
                this.spawnedAt = spawnedAt;
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

        /// <summary>
        /// Returns all objects spawned at a given waypoint node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public SpawnedGameObject[] GetAllSpawnedAt(WaypointNode node)
        {
            return SpawnedGameObjects.Where(_ => _.spawnedAt.GetInstanceID() == node.GetInstanceID()).ToArray();
        }
        
        /// <summary>
        /// Returns all objects spawned at a given waypoint node for a given side.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        public GameObject[] GetAllSpawnedAt(WaypointNode node, Side side)
        {
            return SpawnedGameObjects
                .Where(_ => _.spawnedAt.GetInstanceID() == node.GetInstanceID() && _.side == side)
                .Select(_ => _.gameObject)
                .ToArray();
        }

        /// <summary>
        /// Spawns new game object for given side on first available spawn point
        /// of specified type. Spawned object follows starting waypoint indefinitely.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="prefab"></param>
        /// <param name="spawnType"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SpawnCopy(
            Side side,
            GameObject prefab,
            SpawnType spawnType = SpawnType.Grounded
        )
        {
            switch (side)
            {
                case Side.Attacker:
                    attackerCacheOutdated = true;
                    Spawn(side, prefab, true, spawnType, attackerSpawnPoints);
                    break;
                case Side.Defender:
                    defenderCacheOutdated = true;
                    Spawn(side, prefab, true, spawnType, defenderSpawnPoints);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        
        /// <summary>
        /// Places game object on given side on first available spawn point
        /// of specified type. Spawned object follows starting waypoint indefinitely.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="obj"></param>
        /// <param name="spawnType"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Spawn(
            Side side,
            GameObject obj,
            SpawnType spawnType = SpawnType.Grounded
        )
        {
            switch (side)
            {
                case Side.Attacker:
                    attackerCacheOutdated = true;
                    Spawn(side, obj, false, spawnType, attackerSpawnPoints);
                    break;
                case Side.Defender:
                    defenderCacheOutdated = true;
                    Spawn(side, obj, false, spawnType, defenderSpawnPoints);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        public void Spawn(GameObject obj, WaypointNode spawnAt, Side side, bool instantiateNew = false)
        {
            var t = spawnAt.transform;
            var instance = instantiateNew ? Instantiate(obj) : obj;

            instance.transform.position = t.position;
            instance.transform.rotation = t.rotation;
                
            instance.SetActive(true);
            instance.transform.SetParent(null);
                
            var navigation = instance.GetComponent<AdvancedNavigation>();
            IManagedGameObject managedGameObj = (IManagedGameObject) instance.GetComponent<HealthComponent>() ?? instance.GetOrElseAddComponent<ManagedMonoBehaviour>();
            instance.SetLayerRecursively(LayerMask.NameToLayer(SpawnLayer));

            if (navigation == null)
                throw new NullReferenceException(
                    "Spawned game objects must have an AdvancedNavigation component attached.");

            // run with small delay so the thing has time to think
            navigation.RunAsyncWithDelay(1f, () => navigation.FollowWaypoint(spawnAt));
                
            SpawnedGameObjects.Add(new SpawnedGameObject(side, managedGameObj, spawnAt));
        }

        private void Spawn(Side side, GameObject obj, bool instantiateNew, SpawnType spawnType, List<SpawnPoint> spawnPoints)
        {
            var spawnAt = spawnPoints.Find(_ => _.type == spawnType);
            if (spawnAt != null)
            {
                var t = spawnAt.node.transform;
                var instance = instantiateNew ? Instantiate(obj) : obj;

                instance.transform.position = t.position;
                instance.transform.rotation = t.rotation;
                
                instance.SetActive(true);
                instance.transform.SetParent(null);
                
                var navigation = instance.GetComponent<AdvancedNavigation>();
                
                // either get health component or get/add a generic managed mono behaviour if no health component is present.
                // health components by default expose logic to auto-destruct on death so we can use this here if available.
                IManagedGameObject managedGameObj = instance.GetComponent<HealthComponent>() as IManagedGameObject ?? instance.GetOrElseAddComponent<ManagedMonoBehaviour>();
                instance.SetLayerRecursively(LayerMask.NameToLayer(SpawnLayer));
                
                // set reference to GameplayManager for any behaviours that may require it.
                var gm = FindObjectsOfType<GameplayManager>().First(_ => _.side == side);
                if (gm) foreach (var tree in instance.GetComponents<BehaviorTree>())
                {
                    var gmVariable = tree.GetVariable("GameplayManager");
                    if (gmVariable != null) gmVariable.SetValue(gm.gameObject);
                }
                else Debug.LogError($"No GameplayManager for side {side} found. Behaviour trees that require this reference may encounter errors.");
                
                if (navigation == null)
                    throw new NullReferenceException(
                        "Spawned game objects must have an AdvancedNavigation component attached.");

                // run with small delay so the thing has time to think
                navigation.RunAsyncWithDelay(1f, () => navigation.FollowWaypoint(spawnAt.node));
                
                SpawnedGameObjects.Add(new SpawnedGameObject(side, managedGameObj, spawnAt.node));
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
                    if (spawnedGameObject.managedGameObject.ShouldDestroy()) indicesToRemove.Add(i);
                }

                foreach (var i in indicesToRemove)
                {
                    var spawnedGameObject = SpawnedGameObjects[i];
                    SpawnedGameObjects.RemoveAt(i);
                    GameObject.Destroy(spawnedGameObject.gameObject);
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

        public static IManagedGameObject GetManaged(this GameObject obj)
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
    }
    
    /// <summary>
    /// Extend this class and attach it to game objects
    /// you will spawn using the UnitManager. By creating a custom
    /// implementation of this class you can define the criteria
    /// for destruction of a given object. Simply override the function
    /// ShouldDestroy().
    /// 
    /// Any GameObject can be spawned, but they will be attached with the
    /// default implementation of ManagedMonoBehaviour, which by default
    /// never allows the object to be destroyed.
    /// </summary>
    public class ManagedMonoBehaviour : MonoBehaviour, IManagedGameObject
    {
        public new GameObject gameObject => base.gameObject;

        public virtual bool ShouldDestroy()
        {
            return false;
        }
    }

    public interface IManagedGameObject
    {
        public GameObject gameObject { get; }

        public bool ShouldDestroy();
    }
}
 