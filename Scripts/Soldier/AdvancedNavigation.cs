using System;
using System.Runtime.CompilerServices;
using ForLoopCowboyCommons.EditorHelpers;
using ForLoopCowboyCommons.Environment;
using UnityEngine;
using UnityEngine.AI;

namespace UnityTemplateProjects.forloopcowboy_unity_tools.Scripts.Soldier
{
    /// <summary>
    /// Component that exposes methods for navigating using waypoints.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AdvancedNavigation : MonoBehaviour
    {
        [Tooltip("Maximum speed the nav actor can take.")]
        public float maxSpeed = 3.5f;
        public float maxAngularSpeed = 120f;
        
        [Tooltip("Distance to the waypoint that should be used to consider the waypoint reached.")]
        public float waypointReachedRadius = 1f;
        public WaypointConfiguration waypointConfiguration;

        // cached components
        private NavMeshAgent _navMeshAgent;
        
        private void OnEnable()
        {
            _navMeshAgent = this.GetOrElseAddComponent<NavMeshAgent>();
        }

        public WaypointNode[] GetNearbyWaypointNodes(float maxDistance, float maxHeight, int maxNumNodes)
        {
            var colliders = new Collider[maxNumNodes];
            var layerMask = waypointConfiguration.LayerMask;
            var totalCollidersFound = Physics.OverlapSphereNonAlloc(transform.position, maxDistance, colliders, layerMask);
            var output = new WaypointNode[totalCollidersFound];

            var i = 0;
            foreach (var c in colliders)
            {
                if (c && c.TryGetComponent(out WaypointNode wn))
                {
                    output[i] = wn;
                    i++;
                }
            }

            return output;
        }

        public WaypointNode[] GetNearbyWaypointNodes(float maxDistance, int maxNumNodes) =>
            GetNearbyWaypointNodes(maxDistance, maxDistance, maxNumNodes);
        
        public WaypointNode[] GetNearbyWaypointNodes(float maxDistance) => 
            GetNearbyWaypointNodes(maxDistance, maxDistance, 5);

        public bool MoveTo(Vector3 destination, float speed, float angularSpeed, out NavMeshPath path)
        {
            bool destinationIsAccessible = _navMeshAgent.CalculatePath(destination, path = new NavMeshPath());

            _navMeshAgent.speed = Mathf.Clamp(speed, 0f, maxSpeed);
            _navMeshAgent.angularSpeed = Mathf.Clamp(angularSpeed, 0f, maxAngularSpeed);
            
            if (destinationIsAccessible)
                _navMeshAgent.SetPath(path);

            return destinationIsAccessible;
        }

        public bool MoveTo(Vector3 destination, float speed)
        {
            return MoveTo(destination, speed, maxAngularSpeed, out _);
        }

        public bool MoveTo(Vector3 destination)
        {
            return MoveTo(destination, maxSpeed);
        }

        private Coroutine waypointChecker = null;

        /// <summary>
        /// Moves nav mesh agent to waypoint position
        /// and then the next until it reaches the end, then calls onFinish.
        /// 
        /// If FollowWaypoint is called again, onFinish may never be called as
        /// the agent is overriden and follows a new path.
        /// </summary>
        /// <param name="w">Waypoint to follow</param>
        /// <param name="speed">Velocity of nav mesh component</param>
        /// <param name="onFinish">Callback when game object is close enough to the waypoint.</param>
        public void FollowWaypoint(WaypointNode w, float speed, Action onFinish)
        {
            
            MoveTo(w.transform.position, speed);
            
            if (waypointChecker != null) StopCoroutine(waypointChecker);
            
            // check if close enough to waypoint, then if there's a next waypoint we follow it
            waypointChecker = this.RunAsync(
                () => {},
                () =>
                {
                    bool reached = Vector3.Distance(transform.position, w.transform.position) < waypointReachedRadius;
                    
                    if (reached && w.TryGetNext(out var next))
                        FollowWaypoint(next, speed, onFinish);
                    else if (reached) onFinish();

                    return reached;
                },
                GameObjectHelpers.RoutineTypes.TimeInterval,
                0.25f
            );
        }

        /// <summary>
        /// Moves nav mesh agent to waypoint position
        /// and then the next until it reaches the end, then calls onFinish.
        /// 
        /// If FollowWaypoint is called again, onFinish may never be called as
        /// the agent is overriden and follows a new path.
        /// </summary>
        /// <param name="w">Waypoint to follow</param>
        /// <param name="speed">Velocity of nav mesh component</param>
        /// <param name="neighborsLeftToVisit">Follows only this number of subsequent waypoints.</param>
        /// <param name="onFinish">Callback when game object is close enough to the waypoint.</param>
        public void FollowWaypointUntil(WaypointNode w, float speed, int neighborsLeftToVisit, Action onFinish)
        {

            MoveTo(w.transform.position, speed);
            
            if (waypointChecker != null) StopCoroutine(waypointChecker);

            // check if close enough to waypoint, then if there's a next waypoint we follow it
            waypointChecker = this.RunAsync(
                () => {},
                () =>
                {
                    bool reachedDestination = Vector3.Distance(transform.position, w.transform.position) < waypointReachedRadius;

                    if (reachedDestination)
                    {
                        bool noMoreNeighborsToVisit = neighborsLeftToVisit <= 0;
                        bool hasNext = w.TryGetNext(out var next);
                        
                        // termination: either when no neighbors left or when no next item
                        if (noMoreNeighborsToVisit || !hasNext) onFinish();
                        else FollowWaypointUntil(next, speed, neighborsLeftToVisit - 1, onFinish);
                        
                    }

                    return reachedDestination;
                },
                GameObjectHelpers.RoutineTypes.TimeInterval,
                0.25f
            );
        }

        public void Stop()
        {
            _navMeshAgent.ResetPath();
            if (waypointChecker != null) StopCoroutine(waypointChecker);
        }
    }
}