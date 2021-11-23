using System.Collections;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : MonoBehaviour
    {
        public Bullet Settings;

        public Rigidbody rb;

        // start @ -1 because it fucking bounces the character's hand first
        int bouncesSoFar = -1;

        public virtual void ResetBullet()
        {
            bouncesSoFar = -1;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void OnEnable() {
            if (!GetComponentInChildren<Collider>()) Debug.LogError("Bullet must have a collider");
            rb = gameObject.GetOrElseAddComponent<Rigidbody>();
        }

        public void Fire(Vector3 direction)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(direction.normalized * Settings.muzzleVelocity, ForceMode.VelocityChange);
        }

        private void OnCollisionEnter(Collision other) {
            bouncesSoFar++;

            if (bouncesSoFar == 0) OnFirstImpact(other);

            // enough bounces, disable object
            if (bouncesSoFar >= (Settings?.maxBounces ?? 0)) OnFinalImpact(other);
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

        protected virtual void OnFirstImpact(Collision other){}

        protected virtual void OnImpact(Collision other)
        {
            if (Settings.onImpact)
            {
                var impactExplosion = Instantiate(Settings.onImpact, other.contacts[0].point, Quaternion.identity);
                Destroy(impactExplosion, 3f);
            }
        }

        protected virtual void OnFinalImpact(Collision other)
        {
            gameObject.SetActive(false);
        }

    }
}
