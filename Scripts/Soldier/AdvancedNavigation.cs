using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Environment;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace forloopcowboy_unity_tools.Scripts.Soldier
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

        [SerializeField, ReadOnly] private WaypointNode _lastVisited = null;
        [SerializeField, ReadOnly] private WaypointNode _lastWaypointPathStart = null;
        [SerializeField, ReadOnly] private List<WaypointNode> _lastWaypointPath = new List<WaypointNode>(15);

        /// <summary>
        /// Points to the last visited <c>WaypointNode</c>, or null if no
        /// nodes have been visited yet.
        /// </summary>
        public WaypointNode LastVisited
        {
            get => _lastVisited;
            private set
            {
                LastWaypointPath.Add(value);
                _lastVisited = value;
            }
        }

        /// <summary>
        /// Points to the start of the most recent trajectory, or none if no trajectories have
        /// been followed.
        /// This is updated when the component begins to follow a waypoint chain. 
        /// </summary>
        public WaypointNode LastWaypointPathStart
        {
            get => _lastWaypointPathStart;
            private set
            {
                LastWaypointPath.Clear();
                _lastWaypointPathStart = value;
            }
        }

        /// <summary>
        /// List of all visited nodes in the last waypoint chain followed.
        /// </summary>
        public List<WaypointNode> LastWaypointPath
        {
            get => _lastWaypointPath;
        }
        
        // cached components
        private NavMeshAgent _navMeshAgent;

        /// <summary>
        /// Stores the state of navigation, allowing for pausing and continuing.
        /// </summary>
        [System.Serializable]
        public struct NavigationState
        {
            /// <summary>
            /// Speed at which navigation is occurring.
            /// </summary>
            public float speed { get; }

            /// <summary>
            /// Number of neighbors left to visit in the chain.
            /// When null, visits entire chain.
            /// </summary>
            public int? neighborsLeftToVisit { get; }

            /// <summary>
            /// To be called when navingation reaches the destination.
            /// </summary>
            internal Action terminator { get; }
            
            /// <summary>
            /// Next target to be 
            /// </summary>
            [CanBeNull] public WaypointNode nextTarget { get; private set; }
            
            internal void Deconstruct(
                out float speed,
                out Action terminator,
                [CanBeNull] out WaypointNode nextTarget,
                out int? neighborsLeftToVisit)
            {
                speed = this.speed;
                terminator = this.terminator;
                nextTarget = this.nextTarget;
                neighborsLeftToVisit = this.neighborsLeftToVisit;
            }

            internal NavigationState(WaypointNode nextTarget, float speed, Action terminator, int? neighborsLeftToVisit = null)
            {
                this.nextTarget = nextTarget;
                this.speed = speed;
                this.neighborsLeftToVisit = neighborsLeftToVisit;
                this.terminator = terminator;
            }
            
            internal NavigationState(float speed, Action terminator, int? neighborsLeftToVisit = null) : this(null, speed, terminator, neighborsLeftToVisit) {}
        }

        /// <summary>
        /// State of the navigation component.
        /// When null it means there is no navigation in progress.
        /// This is set when navigation starts and cleared on finish.
        /// </summary>
        public NavigationState? state { get; private set; }

        private void OnEnable()
        {
            _navMeshAgent = this.GetOrElseAddComponent<NavMeshAgent>();
            _animator = this.GetComponent<Animator>();
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
            path = new NavMeshPath();
            
            bool destinationIsAccessible = _navMeshAgent != null && _navMeshAgent.CalculatePath(destination, path);

            if (destinationIsAccessible)
            {
                _navMeshAgent.speed = Mathf.Clamp(speed, 0f, maxSpeed);
                _navMeshAgent.angularSpeed = Mathf.Clamp(angularSpeed, 0f, maxAngularSpeed);
                _navMeshAgent.SetPath(path);
            }

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
            LastWaypointPathStart = w;
            FollowWaypointRec(w, speed, () =>
            {
                state = null;
                onFinish();
            });
        }

        /// <summary>
        /// Follows waypoint at default speed and does nothing when it ends.
        /// </summary>
        /// <param name="w"></param>
        public void FollowWaypoint(WaypointNode w)
        {
            FollowWaypoint(w, 1.5f, () => { });
        }
        
        /// <summary>
        /// Recursive implementation, is unaware of context. When <c>FollowWaypoint</c> is called,
        /// it signifies the beginning of a route follow. This recursive function signifies the
        /// middle/end part of the process. Therefore in order to cache the "WaypointPathStart", we must
        /// segregate these two functions.
        /// </summary>
        /// <param name="w">Waypoint to follow</param>
        /// <param name="speed">Velocity of nav mesh component</param>
        /// <param name="onFinish">Callback when game object is close enough to the waypoint.</param>
        private void FollowWaypointRec(WaypointNode w, float speed, Action onFinish)
        {
            state = new NavigationState(w, speed, onFinish);
            MoveTo(w.transform.position, speed);
            
            if (waypointChecker != null) StopCoroutine(waypointChecker);
            
            // check if close enough to waypoint, then if there's a next waypoint we follow it
            waypointChecker = this.RunAsync(
                () => {},
                () =>
                {
                    bool reached = Vector3.Distance(transform.position, w.transform.position) < waypointReachedRadius;
                    
                    // only set visited when reached
                    if (reached) LastVisited = w;
                    
                    if (reached && w.TryGetNext(out var next))
                        FollowWaypointRec(next, speed, onFinish);
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
            LastWaypointPathStart = w;
            FollowWaypointUntilRec(w, speed, neighborsLeftToVisit, () =>
            {
                state = null;
                onFinish();
            });
        }

        /// <summary>
        /// <see cref="FollowWaypointRec"/>
        /// </summary>
        private void FollowWaypointUntilRec(WaypointNode w, float speed, int neighborsLeftToVisit, Action onFinish)
        {
            state = new NavigationState(w, speed, onFinish, neighborsLeftToVisit);
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
                        LastVisited = w;
                        
                        bool noMoreNeighborsToVisit = neighborsLeftToVisit <= 0;
                        bool hasNext = w.TryGetNext(out var next);
                        
                        // termination: either when no neighbors left or when no next item
                        if (noMoreNeighborsToVisit || !hasNext) onFinish();
                        else FollowWaypointUntilRec(next, speed, neighborsLeftToVisit - 1, onFinish);
                        
                    }

                    return reachedDestination;
                },
                GameObjectHelpers.RoutineTypes.TimeInterval,
                0.25f
            );
        }
        
        /// <returns>True if following a waypoint.</returns>
        public bool IsFollowingPath()
        {
            return state != null && waypointChecker != null;
        }

        /// <returns>True if has a valid navigation state to restore.</returns>
        public bool IsAbleToResumeNavigating(bool coroutineMustBeStopped = false)
        {
            bool isCoroutineStopped = waypointChecker == null;
            bool coroutineStateIsValid = coroutineMustBeStopped ? isCoroutineStopped : true; // either coroutine must be stopped && it is stopped or it's valid.
                
            return state != null && coroutineStateIsValid;
        }
        
        /// <summary>
        /// Resets nav mesh agent's path and stops waypoint checker coroutine.
        /// Keeps state so navigation can be restarted.
        /// </summary>
        /// <returns>Return value of <see cref="IsFollowingPath"/>, true if was able to pause.</returns>
        public bool Pause()
        {
            bool canPause = IsFollowingPath();
            
            if (canPause)
            {
                _navMeshAgent.ResetPath();
                StopCoroutine(waypointChecker);
                waypointChecker = null;
            }
            else
            {
                var statemsg = $"State is {(state == null ? "" : "not")} null";
                var wayptmsg = $"Waypoint checker coroutine is {(waypointChecker == null ? "" : "not")} null";
                Debug.LogWarning($"Cannot pause when nothing is playing! {statemsg}, {wayptmsg}");
            }

            return canPause;
        }

        /// <summary>
        /// If state is defined, re-triggers a follow waypoint
        /// routine using the state information. 
        /// </summary>
        /// <returns>Return value of <see cref="IsAbleToResumeNavigating"/>, true indicating that the navigation was able to resume.</returns>
        public bool Resume()
        {
            bool canResume = IsAbleToResumeNavigating();
            
            if (canResume)
            {
                var (speed, terminator, nextTarget, neighborsLeftToVisit) = (NavigationState) state;
                
                // existence of state.neighborsLeftToVisit implies usage of `FollowWaypointUntil`
                // use recursive call here because we don't want to start a new path - we are simply continuing the last, so no need to reset other variables.
                if (neighborsLeftToVisit != null)
                    FollowWaypointUntilRec(nextTarget, speed, (int) neighborsLeftToVisit, terminator);
                else FollowWaypointRec(nextTarget, speed, terminator);
                

            }
            else
            {
                var statemsg = $"State is {(state == null ? "" : "not")} null";
                Debug.LogWarning($"Cannot resume when state was not saved! {statemsg}.");
            }

            return canResume;
        }

        /// <summary>
        /// Pauses and resets state.
        /// </summary>
        public void Stop()
        {
            Pause();
            state = null;
        }
        
        // Feature: Navigation updates animator

        [Serializable]
        public class AnimatorUpdateSettings
        {
            public bool enabled;
            
            [Tooltip("When true, will update the animator with \"Velocity\" float parameter, normalized.")]
            public bool updateVelocity;
            
        }

        [SerializeField]
        public AnimatorUpdateSettings animatorUpdateSettings;
        private Animator _animator;

        private void Update()
        {
            if (animatorUpdateSettings.enabled)
            {
                if (!_animator)
                {
                    Debug.LogError("Cannot update animator state with no animator.");
                    return;
                }
                
                if (animatorUpdateSettings.updateVelocity)
                    _animator.SetFloat("Velocity", _navMeshAgent.velocity.magnitude / maxSpeed);
                
            }
        }
    }
}