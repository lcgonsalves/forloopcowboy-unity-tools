using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    /// <summary>
    /// Offers functions to detect whether there are soldiers in
    /// a given building and which spawns are available.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeployableBuilding : SerializedMonoBehaviour
    {
        public string BuildingName = "Unknown";
        
        public struct SpawnPoint
        {
            public WaypointNode node;
            
            /// <summary>
            /// When set to true, as long as there is a living soldier that spawned in
            /// this node, this node is unavailable. Set it to false if you want
            /// to be able to spawn many objects in a single position.
            /// </summary>
            public bool singleton;
        }
        
        public List<SpawnPoint> spawnPoints;
        public float waypointCheckRadius = 2f;
        public GameplayManager gameplayManager;

        private HashSet<int> occupantIDs = new HashSet<int>();
        
        [ShowInInspector]
        public int CurrentOccupants => occupantIDs.Count;

        public event Action<int> occupantsChanged;

        public int MaximumOccupants => spawnPoints.Count;
        
        public List<SpawnPoint> GetAvailableSpawnPoints()
        {
            // a soldier only needs to have a health component in it
            // a waypoint node is free if none of the soldiers spawned at said waypoint
            // are alive
            List<SpawnPoint> available = new List<SpawnPoint>();
            
            foreach (var spawnPoint in spawnPoints)
            {
                var allSpawnedAt = gameplayManager.UnitManager.GetAllSpawnedAt(spawnPoint.node, gameplayManager.side);
                
                // if any is alive, skip spawn point
                // if singleton, check for living soldiers
                if (!spawnPoint.singleton || !allSpawnedAt.Any(obj =>
                {
                    var healthComponent = HealthComponent.GetHealthComponent(obj);
                    return healthComponent.IsAlive;
                })) available.Add(spawnPoint);

            }

            return available;
        }

        private BoxCollider trigger;
        
        private void Start()
        {
            trigger = GetComponent<BoxCollider>();
            if (!gameplayManager) gameplayManager = FindObjectOfType<GameplayManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            // we either pick the master of the ragdoll limb or the object itself if the collider is not the limb
            var rdlimb = other.GetComponent<Ragdoll.Limb>();
            var obj = rdlimb ? rdlimb.master.gameObject : other.gameObject;
            var isSoldier = obj is { } && obj.CompareTag(SoldierRandomizer.soldierTag);

            if (isSoldier) // soldier entered
            {
                var id = obj.GetInstanceID();
                var valueChanged = !occupantIDs.Contains(id);
                occupantIDs.Add(id);

                // must be called after adding to reflect the number of current occupants
                if (valueChanged) occupantsChanged?.Invoke(CurrentOccupants);
                
                // if soldier has health component and it dies we reduce occupants
                var healthComponent = obj.GetComponent<HealthComponent>();
                if (healthComponent) healthComponent.onDeath += () => occupantIDs.Remove(id);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var rdlimb = other.GetComponent<Ragdoll.Limb>();
            var obj = rdlimb ? rdlimb.master.gameObject : other.gameObject;
            var isSoldier = obj.CompareTag(SoldierRandomizer.soldierTag);
            
            int? id = obj != null ? obj.GetInstanceID() : (int?) null;
            
            if (isSoldier && id.HasValue && occupantIDs.Contains(id.Value)) // soldier exited
            {
                occupantIDs.Remove(id.Value);
                occupantsChanged?.Invoke(CurrentOccupants);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var waypointNode in spawnPoints)
            {
                Gizmos.color = new Color(0.15f, 0.64f, 0.23f);
                Gizmos.DrawWireSphere(waypointNode.node.transform.position, waypointCheckRadius);
                
                Gizmos.color = Color.red;
                var end = waypointNode.node.GetEnd();
                if (end) Gizmos.DrawWireSphere(end.transform.position, waypointCheckRadius);
            }
        }
    }
    
}