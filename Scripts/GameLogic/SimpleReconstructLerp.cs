using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks.Unity.Timeline;
using forloopcowboy_unity_tools.Scripts.Core;
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

        // for performance, since the match is made by name.
        public (GameObject, GameObject)[] cachedPairs { get; private set; }

        public void Start()
        {
            cachedPairs = new (GameObject, GameObject)[initial.transform.childCount];
            
            for (int i = 0; i < initial.transform.childCount; i++)
            {
                var initChild = initial.transform.GetChild(i);
                var correspondingFinalChild = final.transform.FindRecursively(t => t.name == initChild.name);

                if (initChild && correspondingFinalChild)
                {
                    initChild.gameObject.SetActive(false);
                    correspondingFinalChild.gameObject.SetActive(false);
                    
                    cachedPairs[i] = (initChild.gameObject, correspondingFinalChild.gameObject);
                }
                else Debug.LogError($"Initial child {initChild.name} did not have a corresponding final child. It will be ignored.");
            }
        }

        [Button]
        public void ResetObjects()
        {
            foreach (var pair in cachedPairs)
            {
                pair.Item1.SetActive(false);
                pair.Item2.SetActive(false);
            }
        }
        
        [Button(ButtonStyle.CompactBox)]
        public void SpawnGradually(SimpleReconstructLerp.Position objectToSpawn) { SpawnGradually(objectToSpawn, _ => {}); }

        /// <summary>
        /// Using spawn transition, gradually activates each object of the selected position, calling onSpawn
        /// for each object that gets activated.
        /// </summary>
        /// <param name="objectToSpawn">Either initial or final objects.</param>
        /// <param name="onSpawn">Called after an object is set active.</param>
        public Coroutine SpawnGradually(SimpleReconstructLerp.Position objectToSpawn, Action<(GameObject, GameObject)> onSpawn)
        {
            // yikes this is not super efficient
            void SpawnUpTo(int idxUpperBound)
            {
                for (int i = 0; i < idxUpperBound; i++)
                {
                    var pair = cachedPairs[i];
                    var obj = pair.Get(objectToSpawn);
                    if (obj.activeInHierarchy) continue;

                    obj.SetActive(true);
                    onSpawn(pair);
                }
            }
            
            return spawnTransition.PlayOnceWithDuration(
                this,
                state =>
                {
                    // convert the percentage progress to the max index to activate
                    float percent = state.Snapshot() / spawnTransition.amplitude;
                    int idxUpperBound = Mathf.Clamp(Mathf.CeilToInt((cachedPairs.Length - 1) * percent), 0, cachedPairs.Length - 1);

                    SpawnUpTo(idxUpperBound);
                },
                endState =>
                {
                    SpawnUpTo(cachedPairs.Length);
                },
                overrideSpawnDuration <= 0.001 ? spawnTransition.duration : overrideSpawnDuration
            );
        }
        
        [Button]
        public void SpawnGraduallyAndLerpToDestination(Position from, Position to)
        {
            SpawnGradually(from, spawnedAndTarget => Lerp(from, to, spawnedAndTarget));
        }

        /// <summary>
        /// Moves one object to the other's position using transform position lerp.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="destination"></param>
        /// <param name="pair"></param>
        /// <returns></returns>
        public Coroutine Lerp(Position target, Position destination, (GameObject, GameObject) pair)
        {
            GameObject trgt = pair.Get(target);
                    
            var to = pair.Get(destination).transform;
            var from = pair.Get(destination.Opposite()).transform;
            
            // disable colliders to avoid collisions
            if (trgt.TryGetComponent(out Collider c))
                c.enabled = false;
            
            // make rigidbody kinematic to avoid weirdness
            if (trgt.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = true;
                
            return lerpTransition.LerpTransform(this, trgt.transform, from, to, overrideLerpDuration);
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
        public static GameObject Get(this (GameObject, GameObject) pair, SimpleReconstructLerp.Position position)
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