using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
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

        private HashSet<int> occupantIDs = new HashSet<int>();
        public int CurrentOccupants => occupantIDs.Count;

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
                    foundSoldier = GetHealthComponent(c);
                    if (foundSoldier) break;
                }

                if (foundSoldier) continue; // break early to avoid another overlap sphere / end traversal
                
                var end = node.GetEnd();
                var foundInDefendPoint = end ? Physics.OverlapSphereNonAlloc(end.transform.position, waypointCheckRadius, cache) : 0;
                
                for (int i = 0; i < foundInDefendPoint; i++)
                {
                    var c = cache[i];
                    foundSoldier = GetHealthComponent(c);
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
            var healthComponent = GetHealthComponent(other);

            if (healthComponent) // soldier entered
            {
                var id = healthComponent.GetInstanceID();
                var valueChanged = !occupantIDs.Contains(id);
                occupantIDs.Add(id);

                // must be called after adding to reflect the number of current occupants
                if (valueChanged) occupantsChanged?.Invoke(CurrentOccupants);
                
                // if soldier dies we reduce occupants
                healthComponent.onDeath += () => occupantIDs.Remove(id);
            }
        }

        /// <summary>
        /// Gets health component either in collider, or if the
        /// collider is a Ragdoll.Limb, then we look for the health
        /// component in the master of the puppet.
        /// TODO: maybe cache references? this looks like it could be bad
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private static HealthComponent GetHealthComponent(Collider other)
        {
            var healthComponent = other.gameObject.GetComponent<HealthComponent>();
            if (!healthComponent)
            {
                // if no health component in collider itself, try to see if it's a limb
                // and then look for the component on the root of the object
                var limbComponent = other.gameObject.GetComponent<Ragdoll.Limb>();
                if (limbComponent)
                {
                    healthComponent = limbComponent.master.GetComponent<HealthComponent>();
                    // if still no health component, look in the parent.
                    if (!healthComponent)
                        healthComponent = limbComponent.master.transform.parent.GetComponent<HealthComponent>();
                }
            }

            return healthComponent;
        }

        private void OnTriggerExit(Collider other)
        {
            var healthComponent = GetHealthComponent(other);
            var id = healthComponent != null ? healthComponent.GetInstanceID() : (int?) null;
            
            if (healthComponent && id.HasValue && occupantIDs.Contains(id.Value)) // soldier exited
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
                Gizmos.DrawWireSphere(waypointNode.transform.position, waypointCheckRadius);
                
                Gizmos.color = Color.red;
                var end = waypointNode.GetEnd();
                if (end) Gizmos.DrawWireSphere(end.transform.position, waypointCheckRadius);
            }
        }
    }
    
}