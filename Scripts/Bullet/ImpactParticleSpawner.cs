using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    
    [RequireComponent(typeof(Collider))]
    public class ImpactParticleSpawner : SerializedMonoBehaviour
    {

        public BulletImpactSettings settings = null;

        public float destroyAfterSeconds = 3f;

        private void OnCollisionEnter(Collision other)
        {
            if (
                other.gameObject.TryGetComponent(out BulletController controller) &&
                controller.Settings != null &&
                settings.TryGetParticleFor(controller.Settings, out var collisionParticle)
            )
            {
                var impactPoint = other.GetContact(0).point;
                var impactNormal = other.GetContact(0).normal;

                var instance = Instantiate(collisionParticle, impactPoint, Quaternion.LookRotation(Vector3.forward, impactNormal));
                Destroy(instance, destroyAfterSeconds);
            }
            
        }
    }
}