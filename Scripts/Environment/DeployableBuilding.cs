using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Environment
{
    /// <summary>
    /// Offers functions to detect whether there are soldiers in
    /// a given building and which spawns are available.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeployableBuilding : MonoBehaviour
    {
        public string BuildingName = "Unknown";
        
        public List<WaypointNode> spawnPoints;
        public float waypointCheckRadius = 2f;

        public int CurrentOccupants = 0;

        public event Action<int> occupantsChanged;

        public int MaximumOccupants => spawnPoints.Count;
        
        public WaypointNode[] GetAvailableSpawnPoints()
        {
            // a soldier only needs to have a health component in it
            // a waypoint node is free if nobody is standing at the end or the beginning of the route (KISS)
            Collider[] cache = new Collider[30];
            HashSet<WaypointNode> availableNodes = new HashSet<WaypointNode>();

            foreach (var node in spawnPoints)
            {
                var foundInSpawnPoint = Physics.OverlapSphereNonAlloc(node.transform.position, waypointCheckRadius, cache);
                bool foundSoldier = false;
                
                for (int i = 0; i < foundInSpawnPoint; i++)
                {
                    var c = cache[i];
                    foundSoldier = c.gameObject.GetComponent<HealthComponent>();
                    if (foundSoldier) break;
                }

                if (foundSoldier) continue; // break early to avoid another overlap sphere / end traversal
                
                var end = node.GetEnd();
                var foundInDefendPoint = end ? Physics.OverlapSphereNonAlloc(end.transform.position, waypointCheckRadius, cache) : 0;
                
                for (int i = 0; i < foundInDefendPoint; i++)
                {
                    var c = cache[i];
                    foundSoldier = c.gameObject.GetComponent<HealthComponent>();
                    if (foundSoldier) break;
                }

                if (!foundSoldier) availableNodes.Add(node);
            }

            return availableNodes.ToArray();
        }

        private BoxCollider trigger;
        
        private void Start()
        {
            trigger = GetComponent<BoxCollider>();

            occupantsChanged += i => Debug.Log("Current occupants " + i);
        }

        private void OnTriggerEnter(Collider other)
        {
            var healthComponent = other.gameObject.GetComponent<HealthComponent>();
            if (healthComponent) // soldier entered
            {
                CurrentOccupants++;
                occupantsChanged?.Invoke(CurrentOccupants);
                
                // if soldier dies we reduce occupants
                healthComponent.onDeath += () => CurrentOccupants--;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.GetComponent<HealthComponent>()) // soldier entered
            {
                CurrentOccupants--;
                occupantsChanged?.Invoke(CurrentOccupants);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var waypointNode in spawnPoints)
            {
                Gizmos.color = new Color(0.15f, 0.64f, 0.23f);
                Gizmos.DrawWireSphere(waypointNode.transform.position, waypointCheckRadius);
                
                Gizmos.color = Color.red;
                var end = waypointNode.GetEnd();
                if (end) Gizmos.DrawWireSphere(end.transform.position, waypointCheckRadius);
            }
        }
    }
    
}