using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityCharacterController;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Environment;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

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

        /// <summary> When true, if we have a health component attached, whenever </summary> 
        public bool chaseTargetWhenShot = true;
        
        [Tooltip("Distance to the waypoint that should be used to consider the waypoint reached.")]
        public float waypointReachedRadius = 1f;
        [FormerlySerializedAs("waypointConfiguration")] public WaypointSettings waypointSettings;

        [SerializeField, ReadOnly] private Transform _lastVisited = null;
        [SerializeField, ReadOnly] private Transform _lastWaypointPathStart = null;
        [SerializeField, ReadOnly] private List<Transform> _lastWaypointPath = new List<Transform>(15);

        public NavMeshAgent NavMeshAgent => _navMeshAgent ? _navMeshAgent : GetComponent<NavMeshAgent>();
        
        /// <summary>
        /// Points to the last visited position, or null if no
        /// nodes have been visited yet.
        /// </summary>
        public Transform LastVisited
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
        public Transform LastWaypointPathStart
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
        public List<Transform> LastWaypointPath
        {
            get => _lastWaypointPath;
        }

        public bool showDebugMessages = false;
        
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
            [CanBeNull] public Transform nextTarget { get; private set; }
            
            internal void Deconstruct(
                out float speed,
                out Action terminator,
                [CanBeNull] out Transform nextTarget,
                out int? neighborsLeftToVisit)
            {
                speed = this.speed;
                terminator = this.terminator;
                nextTarget = this.nextTarget;
                neighborsLeftToVisit = this.neighborsLeftToVisit;
            }

            internal NavigationState(Transform nextTarget, float speed, Action terminator, int? neighborsLeftToVisit = null)
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

        private void Start()
        {
            if (chaseTargetWhenShot && TryGetComponent(out HealthComponent healthComponent))
            {
                healthComponent.onDamage += (_, src) => MoveToTransformWhenDamaged(healthComponent, src);
            }
            
        }

        private void MoveToTransformWhenDamaged(HealthComponent healthComponent, IDamageProvider source)
        {
            // don't move if dead
            if (healthComponent.IsDead) return;
            
            bool IsCurrentlyChasingDamageSource(BulletController dmgSource) => state.HasValue && state.Value.nextTarget != null && dmgSource.firedBy != null && state.Value.nextTarget.GetInstanceID() != dmgSource.firedBy.GetInstanceID(); // no need to continue chasing if already chasing target

            var bulletController = source as BulletController;
            bool damageSourceIsBulletController = source != null && bulletController is { };
            bool bulletHasASource = damageSourceIsBulletController && bulletController.firedBy != null;

            if (bulletHasASource && !IsCurrentlyChasingDamageSource(bulletController))
            {
                MoveToTransform(bulletController.firedBy.transform, maxSpeed, 120f);
            }
        }

        private void OnEnable()
        {
            _navMeshAgent = this.GetOrElseAddComponent<NavMeshAgent>();
            _animator = this.GetComponent<Animator>();
        }

        public WaypointNode[] GetNearbyWaypointNodes(float maxDistance, float maxHeight, int maxNumNodes)
        {
            var colliders = new Collider[maxNumNodes];
            var layerMask = waypointSettings.LayerMask;
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

        /// <summary>
        /// Pausable move to transform.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="speed"></param>
        /// <param name="angularSpeed"></param>
        public void MoveToTransform(Transform destination, float speed, float angularSpeed)
        {
            if (
                destination == null ||
                !destination.hasChanged && _navMeshAgent.velocity.magnitude < 0.001f
            ) return;
            
            var correctedSpeed = Mathf.Clamp(speed, 0f, maxSpeed);
            state = new NavigationState(destination, correctedSpeed, () => { });
            
            MoveTo(destination.position, correctedSpeed, angularSpeed);
        }

        public void MoveTo(Vector3 destination, float speed, float angularSpeed)
        {

            if (NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.speed = Mathf.Clamp(speed, 0f, maxSpeed);;
                NavMeshAgent.angularSpeed = angularSpeed;
                NavMeshAgent.destination = destination;
            }
            else Debug.Log($"{name} not on navmesh! Fuck!!");
            
        }

        public void MoveTo(Vector3 destination, float speed)
        {
            MoveTo(destination, speed, maxAngularSpeed);
        }

        public void MoveTo(Vector3 destination)
        {
            MoveTo(destination, maxSpeed);
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
        public void FollowWaypoint(WaypointNode w, float speed, Action onFinish) =>
            FollowWaypoint(w.transform, speed, onFinish);
        
        public void FollowWaypoint(Transform w, float speed, Action onFinish)
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
        /// <param name="nextPosition">Waypoint to follow</param>
        /// <param name="speed">Velocity of nav mesh component</param>
        /// <param name="onFinish">Callback when game object is close enough to the waypoint.</param>
        private void FollowWaypointRec(Transform nextPosition, float speed, Action onFinish)
        {
            var wptTransform = nextPosition.transform;
            
            state = new NavigationState(wptTransform, speed, onFinish);
            MoveTo(wptTransform.position, speed);
            
            if (waypointChecker != null) StopCoroutine(waypointChecker);
            
            // check if close enough to waypoint, then if there's a next waypoint we follow it
            waypointChecker = this.RunAsync(
                () => {},
                () =>
                {
                    bool reached = Vector3.Distance(transform.position, nextPosition.position) < waypointReachedRadius;
                    
                    // only set visited when reached
                    if (reached) LastVisited = nextPosition.transform;
                    
                    if (reached && nextPosition.TryGetComponent(out WaypointNode w) && w.TryGetNext(out var next))
                        FollowWaypointRec(next.transform, speed, onFinish);
                    
                    else if (reached) onFinish();

                    return reached;
                },
                GameObjectHelpers.RoutineTypes.TimeInterval,
                delay: 0.25f
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
        public void FollowWaypointUntil(WaypointNode w, float speed, int neighborsLeftToVisit, Action onFinish) =>
            FollowWaypointUntil(w.transform, speed, neighborsLeftToVisit, onFinish);

        public void FollowWaypointUntil(Transform nextPosition, float speed, int neighborsLeftToVisit, Action onFinish)
        {
            LastWaypointPathStart = nextPosition;
            
            FollowWaypointUntilRec(nextPosition, speed, neighborsLeftToVisit, () =>
            {
                state = null;
                onFinish();
            });
        }

        /// <summary>
        /// <see cref="FollowWaypointRec"/>
        /// </summary>
        private void FollowWaypointUntilRec(Transform nextNode, float speed, int neighborsLeftToVisit, Action onFinish)
        {
            var wptTransform = nextNode.transform;
            
            state = new NavigationState(wptTransform, speed, onFinish, neighborsLeftToVisit);
            MoveTo(wptTransform.position, speed);
            
            if (waypointChecker != null) StopCoroutine(waypointChecker);

            // check if close enough to waypoint, then if there's a next waypoint we follow it
            waypointChecker = this.RunAsync(
                () => {},
                () =>
                {
                    bool reachedDestination = Vector3.Distance(transform.position, nextNode.transform.position) < waypointReachedRadius;

                    if (reachedDestination)
                    {
                        LastVisited = nextNode;

                        WaypointNode next = null;
                        bool noMoreNeighborsToVisit = neighborsLeftToVisit <= 0;
                        bool hasNext = nextNode.TryGetComponent(out WaypointNode w) && w.TryGetNext(out next);
                        
                        // termination: either when no neighbors left or when no next item
                        if (noMoreNeighborsToVisit || !hasNext) onFinish();
                        else if (next) FollowWaypointUntilRec(next.transform, speed, neighborsLeftToVisit - 1, onFinish);
                        
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
            return state != null || waypointChecker != null || _navMeshAgent.hasPath;
        }

        /// <returns>True if has a valid navigation state to restore.</returns>
        public bool IsAbleToResumeNavigating(bool navigationMustBeStopped = false)
        {
            bool isCoroutineStopped = waypointChecker == null;
            bool coroutineStateIsValid = navigationMustBeStopped ? isCoroutineStopped : true; // either coroutine must be stopped && it is stopped or it's valid.
                
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
                
                if (waypointChecker != null)
                {
                    StopCoroutine(waypointChecker);
                    waypointChecker = null;
                }
            }
            else if (showDebugMessages)
            {
                var statemsg = $"State is {(state == null ? "" : "not")} null";
                var wayptmsg = $"Waypoint checker coroutine is {(waypointChecker == null ? "" : "not")} null";
                Debug.LogWarning($"Cannot pause when nothing is playing! {statemsg}, {wayptmsg}");
            }

            return canPause;
        }

        /// <summary>
        /// If state is defined and the object is currently stopped, re-triggers a follow waypoint
        /// routine using the state information.
        /// </summary>
        /// <returns>Return value of <see cref="IsAbleToResumeNavigating"/>, true indicating that the navigation was able to resume.</returns>
        public bool Resume()
        {
            // if coroutine is running (i.e. we're following a waypont) canResume will return false.
            bool canResume = IsAbleToResumeNavigating(navigationMustBeStopped: true);
            
            if (canResume)
            {
                var (speed, terminator, nextTarget, neighborsLeftToVisit) = (NavigationState) state; // null check is in [[IsAbleToResumeNavigating]] 
                
                // existence of state.neighborsLeftToVisit implies usage of `FollowWaypointUntil`
                // use recursive call here because we don't want to start a new path - we are simply continuing the last, so no need to reset other variables.
                if (neighborsLeftToVisit != null)
                    FollowWaypointUntilRec(nextTarget, speed, (int) neighborsLeftToVisit, terminator);
                else FollowWaypointRec(nextTarget, speed, terminator);
                

            }
            else if (showDebugMessages)
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

        public void StopAndDisable()
        {
            Stop();
            _navMeshAgent.enabled = false;
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