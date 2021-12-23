using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime.Tasks.Unity.Timeline;
using forloopcowboy_unity_tools.Scripts.Core;
using RayFire;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    
    /// <summary>
    /// Given two objects with children of the same name,
    /// can lerp objects from one position to the other.
    ///
    /// Think of it as the lego building algorithm from the lego games.
    /// </summary>
    [SelectionBase]
    public class SimpleReconstructLerp : SerializedMonoBehaviour
    {
        public GameObject initial;
        public GameObject final;

        [FoldoutGroup("Movement Settings")]
        public Transition lerpTransition;

        [FoldoutGroup("Movement Settings")]
        public float overrideLerpDuration;

        [FoldoutGroup("Spawn Settings")]
        public Transition spawnTransition;

        [FoldoutGroup("Spawn Settings")] 
        public float overrideSpawnDuration;
        
        public enum Position
        {
            Initial,
            Final
        }
        
        public struct LocationRotation
        {
            public Vector3 position;
            public Quaternion rotation;
            public Transform transform;

            public LocationRotation(Transform t)
            {
                this.position = t.position;
                this.rotation = t.rotation;
                this.transform = t;
            }
        }

        // for performance, since the match is made by name.
        public GameObject[] cachedParticles { get; private set; }

        public (LocationRotation, LocationRotation)[] startAndEndPosition;
        
        public int totalParticles => cachedParticles.Length;
        public int particlesInPlace = 0;
        public float percentInPlace => (float) particlesInPlace / (float) totalParticles;

        public void Initialize()
        {
            var copy = Instantiate(initial, initial.transform.parent, true);
            copy.name = "TransientParticleRoot";
            
            var childCount = copy.transform.childCount;
            
            cachedParticles = new GameObject[childCount];
            startAndEndPosition = new (LocationRotation, LocationRotation)[childCount];
            
            for (int i = 0; i < copy.transform.childCount; i++)
            {
                var initChild = copy.transform.GetChild(i);
                var correspondingFinalChild = final.transform.FindRecursively(t => t.name == initChild.name);

                if (initChild && correspondingFinalChild)
                {
                    var correspondingOriginal = initial.transform.GetChild(i);
                    
                    initChild.gameObject.SetActive(false);
                    correspondingFinalChild.gameObject.SetActive(false);
                    correspondingOriginal.gameObject.SetActive(false);

                    cachedParticles[i] = initChild.gameObject;
                    startAndEndPosition[i] = (
                        new LocationRotation(correspondingOriginal),
                        new LocationRotation(correspondingFinalChild)
                    );
                }
                else Debug.LogError($"Initial child {initChild.name} did not have a corresponding final child. It will be ignored.");
            }
        }

        [Button]
        public void ResetObjects()
        {
            foreach (var o in cachedParticles)
            {
                o.SetActive(false);
            }
        }
        
        [Button(ButtonStyle.CompactBox)]
        public void SpawnGradually(SimpleReconstructLerp.Position startinPosition) { SpawnGradually(startinPosition, (_, __) => {}); }

        /// <summary>
        /// Using spawn transition, gradually activates each object of the selected position, calling onSpawn
        /// for each object that gets activated.
        /// </summary>
        /// <param name="startinPosition">Either initial or final objects.</param>
        /// <param name="onSpawn">Called after an object is set active.</param>
        public Coroutine SpawnGradually(SimpleReconstructLerp.Position startinPosition, Action<GameObject, int> onSpawn)
        {
            // yikes this is not super efficient
            void SpawnUpTo(int idxUpperBound)
            {
                for (int i = 0; i < idxUpperBound; i++)
                {
                    var spawned = cachedParticles[i];
                    if (spawned.activeInHierarchy) continue;

                    var settings = startAndEndPosition[i].Get(startinPosition);

                    spawned.transform.position = settings.position;
                    spawned.transform.rotation = settings.rotation;
                    
                    spawned.SetActive(true);
                    onSpawn(spawned, i);
                }
            }
            
            return spawnCoroutine = spawnTransition.PlayOnceWithDuration(
                this,
                state =>
                {
                    // convert the percentage progress to the max index to activate
                    float percent = state.Snapshot() / spawnTransition.amplitude;
                    int idxUpperBound = Mathf.Clamp(Mathf.CeilToInt((cachedParticles.Length - 1) * percent), 0, cachedParticles.Length - 1);

                    SpawnUpTo(idxUpperBound);
                },
                endState =>
                {
                    SpawnUpTo(cachedParticles.Length);
                },
                overrideSpawnDuration <= 0.001 ? spawnTransition.duration : overrideSpawnDuration
            );
        }

        public Dictionary<GameObject, Coroutine> lerpsHappening { get; private set;  } =
            new Dictionary<GameObject, Coroutine>();

        public Coroutine spawnCoroutine;

        [Button]
        public void SpawnGraduallyAndLerpToDestination(Position to)
        {
            particlesInPlace = 0;
            
            SpawnGradually(
                to.Opposite(),
                (spawned, idx) =>
                {
                    InterruptLerp(spawned); // spawned 

                    lerpsHappening.Add(
                        spawned,
                        Lerp(
                            spawned,
                            to,
                            startAndEndPosition[idx],
                     () =>
                            {
                                lerpsHappening.Remove(spawned);
                                particlesInPlace++;
                            })
                    );
                });
        }

        [Button]
        public void InterruptAll()
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            
            foreach (var coroutine in lerpsHappening)
            {
                StopAndEnablePhysics(coroutine.Key, coroutine.Value);
            }
        }

        public void InterruptLerp(GameObject objectThatIsMoving)
        {
            if (lerpsHappening.TryGetValue(objectThatIsMoving, out var coroutine) && coroutine != null)
            {
                StopAndEnablePhysics(objectThatIsMoving, coroutine);
            }
        }

        public void StopAndEnablePhysics(GameObject objectThatIsMoving, Coroutine coroutine = null)
        {
            if (coroutine != null) StopCoroutine(coroutine);

            if (objectThatIsMoving.TryGetComponent(out RayfireRigid r))
            {
                r.Initialize();
                r.Activate();
            }
                
            if (objectThatIsMoving.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
            }
        }

        /// <summary>
        /// Moves one object to the other's position using transform position lerp.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="destination"></param>
        /// <param name="pair"></param>
        /// <returns></returns>
        public Coroutine Lerp(GameObject target, Position destination, (LocationRotation, LocationRotation) pair, Action onFinish)
        {
            var to = pair.Get(destination).transform;
            var from = target.transform;
            
            // make rigidbody kinematic to avoid weirdness
            if (target.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = true;
                
            return lerpTransition.LerpTransform(this, target.transform, from, to, overrideLerpDuration, onFinish);
        }
        
    }

    static class GameObjectPairExtension
    {

        /// <summary>
        /// Returns left item if initial, and right if final.
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static T Get<T>(this (T, T) pair, SimpleReconstructLerp.Position position)
        {
            switch (position)
            {
                case SimpleReconstructLerp.Position.Initial:
                    return pair.Item1;
                    break;
                case SimpleReconstructLerp.Position.Final:
                    return pair.Item2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        public static SimpleReconstructLerp.Position Opposite(this SimpleReconstructLerp.Position p)
        {
            switch (p)
            {
                case SimpleReconstructLerp.Position.Initial:
                    return SimpleReconstructLerp.Position.Final;
                    break;
                case SimpleReconstructLerp.Position.Final:
                    return SimpleReconstructLerp.Position.Initial;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(p), p, null);
            }
        }
        
    }
}