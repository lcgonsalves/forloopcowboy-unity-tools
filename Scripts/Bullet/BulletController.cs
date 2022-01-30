using System;
using System.Collections;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile;
using JetBrains.Annotations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    
    [RequireComponent(typeof(Rigidbody)), SelectionBase]
    public class BulletController : MonoBehaviour, IDamageProvider
    {
        public Bullet Settings;

        public Rigidbody rb;

        /// <summary>
        /// If bullet was fired by somebody, it will be set here.
        /// For bullets fired anonymously, this value will be null.
        /// </summary>
        [CanBeNull] public GameObject firedBy = null;

        // start @ -1 because it fucking bounces the character's hand first
        int bouncesSoFar = -1;

        /// <summary>
        /// When set to false, bouncesSoFar is not incremented, therefore
        /// if this is set to false before the final impact, it will never
        /// call that function.
        /// </summary>
        public bool countBounces = true;

        public virtual void ResetBullet()
        {
            bouncesSoFar = -1;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = rb.isKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;
        }

        private void OnEnable() {
            if (!GetComponentInChildren<Collider>()) Debug.LogError("Bullet must have a collider");
            rb = gameObject.GetOrElseAddComponent<Rigidbody>();
        }

        private void OnDisable()
        {
            ResetBullet();
        }

        public void Fire(Vector3 direction)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.velocity = Vector3.zero;
            rb.AddForce(direction.normalized * Settings.muzzleVelocity, ForceMode.VelocityChange);
        }

        private void OnCollisionEnter(Collision other) {
            if (countBounces) bouncesSoFar++;

            if (bouncesSoFar == 0) OnFirstImpact(other);
            // enough bounces, disable object
            else if (bouncesSoFar >= (Settings != null ? Settings.maxBounces : 0)) OnFinalImpact(other);
            else OnImpact(other); // on impact is called only while bounces is < max
        }

        private Coroutine killSequence = null;

        // Initiates kill sequence if one has not been initiated. Does not override. CancelKillSequence() first.
        public void InitiateKillSequence(float delayInSeconds)
        {
            if (killSequence == null && gameObject.activeSelf)
            {
                killSequence = StartCoroutine(KillIn(delayInSeconds));
            }
        }

        // Stops coroutine if one has been started.
        public void CancelKillSequence()
        {
            if (killSequence != null)
            {
                StopCoroutine(killSequence);
                killSequence = null;
            }
        }

        private IEnumerator KillIn(float delayInSeconds)
        {
            yield return new WaitForSeconds(delayInSeconds);
            gameObject.SetActive(false);
        }

        /// <summary> Called on first counted impact. </summary>
        protected virtual void OnFirstImpact(Collision other)
        {
            if (Settings.spawnOnFirstImpact) SpawnImpactExplosion(other);
            onFirstImpact?.Invoke(other);
        }
        public event Action<Collision> onFirstImpact;

        /// <summary> Called on every impact, including first and last. </summary>
        protected virtual void OnImpact(Collision other)
        {
            GameObject impactExplosion = null;
            if (Settings.spawnOnImpact) impactExplosion = SpawnImpactExplosion(other);
            
            onImpact?.Invoke(other, impactExplosion);
        }

        /// <summary> First param is the collision, second is a game object instance of the Impact explosion, if one is defined (null check!). </summary>
        public event Action<Collision, GameObject> onImpact;

        /// <summary> Called on final counted impact. </summary>
        protected virtual void OnFinalImpact(Collision other)
        {
            OnImpact(other);
            gameObject.SetActive(false);
            
            if (Settings.spawnOnFinalImpact)
                SpawnImpactExplosion(other);
            
            onFinalImpact?.Invoke(other);
        }
        
        /// <summary> Invoked after onImpact, and after object is disabled. </summary>
        public event Action<Collision> onFinalImpact;

        public int GetDamageAmount()
        {
            return Settings != null ? Settings.GetDamageAmount() : 0;
        }
        
        private GameObject SpawnImpactExplosion(Collision other)
        {
            GameObject impactExplosion = null;
            
            if (Settings.onImpactPrefab)
            {
                impactExplosion = Instantiate(Settings.onImpactPrefab, other.contacts[0].point, Quaternion.identity);
                Destroy(impactExplosion, 3f);
            }

            return impactExplosion;
        }
    }
}
