using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core.Networking.forloopcowboy_unity_tools.Scripts.Core.Networking;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core.Networking
{
    /// <summary>
    /// Poolable object.
    /// Exposes collision events.
    /// Auto destroys / pools based on a lifetime check that gets reset after every bounce.
    /// Auto destroys / pools immediately once maxBounces is reached.
    /// </summary>
    [RequireComponent(typeof(Rigidbody)), SelectionBase]
    public class NetworkProjectile : SimpleDamageProvider
    {
        [InlineEditor(InlineEditorModes.FullEditor)]
        public ProjectileFXSettings fxSettings;

        [ValidateInput("PrefabMustBeDefinedForPool", "If prefab is not defined, object cannot be pooled in network pool, and will be disabled on lifetime countdown end.", InfoMessageType.Warning)]
        public GameObject prefab;
        private bool PrefabMustBeDefinedForPool(GameObject pf) => pf != null;
        
        public float MaxLifetimeAfterBounceSeconds = 5f;
        private Coroutine deathCountdown = null;
        
        private Rigidbody rb;

        /// <summary>
        /// If bullet was fired by somebody, it will be set here.
        /// For bullets fired anonymously, this value will be null.
        /// </summary>
        [CanBeNull, ReadOnly] public GameObject firedBy = null;

        // start @ -1 because counter is incremented at start.
        int bouncesSoFar = -1;
        public int maxBounces = 10;

        /// <summary>
        /// When set to false, bouncesSoFar is not incremented, therefore
        /// if this is set to false before the final impact, it will never
        /// call that function.
        /// </summary>
        public bool countBounces = true;
        
        // Impact event propagators //
        
        /// <summary>Invoked on first impact, and first impact only! First param is the collision,
        /// second are the effects spawned on this impact.</summary>
        public event Action<Collision, IEnumerable<GameObject>> onFirstImpact;
        
        /// <summary>Invoked on all impacts, except first and last. First param is the collision,
        /// second are the effects spawned on this impact. </summary>
        public event Action<Collision, IEnumerable<GameObject>> onImpact;
        
        /// <summary>Invoked only on last counted impact. Invoked after onImpact, and after object is disabled. First param is the collision,
        /// second are the effects spawned on this impact.</summary>
        public event Action<Collision, IEnumerable<GameObject>> onFinalImpact;
        
        public virtual void ResetBullet(int? maxBounces = null)
        {
            bouncesSoFar = -1;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = rb.isKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;

            if (maxBounces.HasValue)
                this.maxBounces = maxBounces.Value;
        }

        private void OnEnable() {
            if (!GetComponentInChildren<Collider>()) Debug.LogError("Bullet must have a collider");
            rb = gameObject.GetOrElseAddComponent<Rigidbody>();
        }

        private void OnDisable()
        {
            ResetBullet();
        }

        public void Fire(Vector3 velocity)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.velocity = Vector3.zero;
            rb.AddForce(velocity, ForceMode.VelocityChange);
        }
        
        public void Fire(Vector3 direction, float velocity) => Fire(direction.normalized * velocity);

        private void OnCollisionEnter(Collision other) {
            if (countBounces) bouncesSoFar++;
            
            if (bouncesSoFar == 0) OnFirstImpact(other);
            // enough bounces, disable object
            else if (bouncesSoFar == maxBounces) OnFinalImpact(other);
            else if (bouncesSoFar < maxBounces) OnImpact(other);
        }
        
        /// <summary>
        /// Initiates countdown to return object to pool.
        /// Make sure to cancel countdown before calling this, or call RestartDeathCountdown.
        /// </summary>
        private void StartDeathCountdown()
        {
            deathCountdown = this.RunAsyncWithDelay(
                MaxLifetimeAfterBounceSeconds,
                ReturnToPool
            );
        }

        private void CancelDeathCountdown() => deathCountdown.IfNotNull(StopCoroutine);
        
        /// <summary>
        /// Resets counter to re-pool object.
        /// </summary>
        private void RestartDeathCountdown()
        {
            CancelDeathCountdown();
            StartDeathCountdown();
        }
        
        private void ReturnToPool()
        {
            ReturnToPoolServerRpc();
        }

        private void ReturnToPoolLocal()
        {
            var netPool = NetworkObjectPool.Singleton;

            if (netPool != null)
                NetworkObjectPool.Singleton.ReturnNetworkObject(
                    GetComponent<NetworkObject>(),
                    prefab
                );
            else 
                gameObject.SetActive(false);
        }

        [ServerRpc]
        private void ReturnToPoolServerRpc() => ReturnToPoolLocal();

        /// <summary> Called on first counted impact. </summary>
        protected virtual void OnFirstImpact(Collision other)
        {
            RestartDeathCountdown();
            var spawnedFx = SpawnImpactParticles(other, fxSettings.FirstImpactFX);
            
            onFirstImpact?.Invoke(
                other,
                spawnedFx
            );
        }

        /// <summary> Called on every impact, including first and last. </summary>
        protected virtual void OnImpact(Collision other)
        {
            RestartDeathCountdown();
            var spawnedFx = SpawnImpactParticles(other, fxSettings.ImpactFX);
            
            onImpact?.Invoke(
                other,
                spawnedFx
            );
        }

        /// <summary> Called on final counted impact. Immediately returns object to pool. </summary>
        protected virtual void OnFinalImpact(Collision other)
        {
            CancelDeathCountdown();
            var spawnedFx = SpawnImpactParticles(other, fxSettings.LastImpactFX, forceSpawn: true); // always spawn on last impact
            
            onFinalImpact?.Invoke(
                other,
                spawnedFx
            );
            
            ReturnToPool();
        }

        // to prevent a particle from being spawned too often
        private SpamProtectedGameObjectInstantiator safeSpawner = SpamProtection.ObjectInstantiator(0.5f);
        
        /// <summary>
        /// Spawns FX particles.
        /// </summary>
        /// <param name="forceSpawn">If set to true, will not use spam protected spawner.</param>
        /// <returns>Objects spawned.</returns>
        private IEnumerable<GameObject> SpawnImpactParticles(Collision other, IEnumerable<ProjectileFX> effects, bool forceSpawn = false)
        {
            var collisionContact = other.contacts[0];
            var spawned = new List<GameObject>();
            
            foreach (ProjectileFX fx in effects)
            {
                // do not spawn if threshold is not met
                if (other.relativeVelocity.magnitude < fx.velocitySpawnThreshold) continue;

                bool hasCreatedInstance = forceSpawn;
                GameObject instance = null;

                if (forceSpawn)
                {
                    instance = Instantiate(
                        fx.prefab,
                        collisionContact.point,
                        fx.orientToCollisionNormal
                            ? Quaternion.LookRotation(collisionContact.normal)
                            : Quaternion.identity
                    );
                }
                else
                {
                    safeSpawner.SafeInstantiate(
                        fx.prefab,
                        collisionContact.point,
                        fx.orientToCollisionNormal
                            ? Quaternion.LookRotation(collisionContact.normal)
                            : Quaternion.identity,
                        out instance
                    );
                }
                
                if (hasCreatedInstance)
                {
                    if (fx.despawnSettings.destroyAutomatically)
                        Destroy(instance, fx.despawnSettings.destroyDelay);
                
                    spawned.Add(instance);
                }
            }

            return spawned;
        }

    }
}