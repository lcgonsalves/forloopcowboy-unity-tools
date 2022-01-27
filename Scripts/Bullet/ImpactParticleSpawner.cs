using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    
    [RequireComponent(typeof(Collider))]
    public class ImpactParticleSpawner : SerializedMonoBehaviour
    {

        public BulletImpactSettings settings = null;
        public float destroyAfterSeconds = 3f;

        private HealthComponent _healthComponent;
        private Ragdoll.Limb _limbComponent;

        private HealthComponent healthComponent =>
            _healthComponent ? _healthComponent : _healthComponent = limbComponent?.master.gameObject.GetComponent<HealthComponent>();
        
        private Ragdoll.Limb limbComponent =>
            _limbComponent ? _limbComponent : _limbComponent = GetComponent<Ragdoll.Limb>();
        

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

                // todo: maybe move this shit
                if (healthComponent && limbComponent)
                {
                    // todo: actually take into account the multiplier
                    healthComponent.Damage(controller.Settings.GetDamageAmount(), controller);
                }

                var instance = Instantiate(collisionParticle, impactPoint, Quaternion.LookRotation(Vector3.forward, impactNormal));
                Destroy(instance, destroyAfterSeconds);
            }
            
        }
    }
}