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
using UnityEngine.SubsystemsImplementation;

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
            var netObj  = GetComponent<NetworkObject>();
            
            netObj.Despawn(false); // do not destroy as object is returning to pool

            if (netPool != null)
                NetworkObjectPool.Singleton.ReturnNetworkObject(
                    netObj,
                    prefab
                );
            else 
                gameObject.SetActive(false);
        }

        [ServerRpc]
        private void ReturnToPoolServerRpc() => ReturnToPoolLocal();

        private enum ImpactType
        {
            First,
            Last,
            Regular
        }

        /// <summary> Called on first counted impact. </summary>
        protected virtual void OnFirstImpact(Collision other)
        {
            RestartDeathCountdown();
            
            Vector3 collisionContactPoint = other.contacts[0].point;
            Vector3 collisionContactNormal = other.contacts[0].normal;
            float impactVelocity = other.relativeVelocity.magnitude;
            
            SpawnImpactParticlesClientRpc(collisionContactPoint, collisionContactNormal, impactVelocity, ImpactType.First);
        }

        /// <summary> Called on every impact, including first and last. </summary>
        protected virtual void OnImpact(Collision other)
        {
            RestartDeathCountdown();

            Vector3 collisionContactPoint = other.contacts[0].point;
            Vector3 collisionContactNormal = other.contacts[0].normal;
            float impactVelocity = other.relativeVelocity.magnitude;
            
            SpawnImpactParticlesClientRpc(collisionContactPoint, collisionContactNormal, impactVelocity, ImpactType.Regular);
        }

        /// <summary> Called on final counted impact. Immediately returns object to pool. </summary>
        protected virtual void OnFinalImpact(Collision other)
        {
            CancelDeathCountdown();
            
            Vector3 collisionContactPoint = other.contacts[0].point;
            Vector3 collisionContactNormal = other.contacts[0].normal;
            float impactVelocity = other.relativeVelocity.magnitude;
            
            SpawnImpactParticlesClientRpc(collisionContactPoint, collisionContactNormal, impactVelocity, ImpactType.Last, forceSpawn: true); // always spawn on last impact
            ReturnToPool();
        }

        // to prevent a particle from being spawned too often
        private SpamProtectedGameObjectInstantiator safeSpawner = SpamProtection.ObjectInstantiator(0.5f);
        
        /// <summary>
        /// Spawns FX particles.
        /// </summary>
        /// <param name="forceSpawn">If set to true, will not use spam protected spawner.</param>
        /// <returns>Objects spawned.</returns>
        [ClientRpc]
        private void SpawnImpactParticlesClientRpc(
            Vector3 collisionContactPoint,
            Vector3 collisionContactNormal,
            float impactVelocity,
            ImpactType impactType,
            bool forceSpawn = false
        )
        {
            IEnumerable<ProjectileFX> effects;

            switch (impactType)
            {
                case ImpactType.First:
                    effects = fxSettings.FirstImpactFX;
                    break;
                case ImpactType.Last:
                    effects = fxSettings.LastImpactFX;
                    break;
                case ImpactType.Regular:
                    effects = fxSettings.ImpactFX;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(impactType), impactType, null);
            }

            foreach (ProjectileFX fx in effects)
            {
                // do not spawn if threshold is not met
                if (impactVelocity < fx.velocitySpawnThreshold) continue;

                bool hasCreatedInstance = forceSpawn;
                GameObject instance = null;

                if (forceSpawn)
                {
                    instance = Instantiate(
                        fx.prefab,
                        collisionContactPoint,
                        fx.orientToCollisionNormal
                            ? Quaternion.LookRotation(collisionContactNormal)
                            : Quaternion.identity
                    );
                }
                else
                {
                    safeSpawner.SafeInstantiate(
                        fx.prefab,
                        collisionContactPoint,
                        fx.orientToCollisionNormal
                            ? Quaternion.LookRotation(collisionContactNormal)
                            : Quaternion.identity,
                        out instance
                    );
                }

                if (hasCreatedInstance)
                {
                    var netObj = instance.GetComponent<NetworkObject>();
                    var autoDespawn = instance.GetOrElseAddComponent<AutoDespawn>();
                    
                    // spawn, then despawn automatically
                    netObj.Spawn();
                    autoDespawn.DespawnIn(fx.despawnSettings.destroyDelay);
                }
            }
        }

    }
}