using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks.Unity.Timeline;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile
{
    
    [CreateAssetMenu(fileName = "Guided Bullet Spell", menuName = "Spells/Guided Bullet Spell", order = 1)]
    public class GuidedBulletDeprecatedSpell : BulletDeprecatedSpell
    {

        public float radius;
        public float stopRadius;
        public float force;
        
        private void OnEnable()
        {
            OnBulletFired += OnBulletFiredHandler;
        }

        private void OnDisable()
        {
            OnBulletFired -= OnBulletFiredHandler;
        }

        private void OnBulletFiredHandler(SpellUserBehaviour caster, BulletController controller)
        {
            // no target, no guide
            if (!caster.GetTarget(this, out var target)) return;
            
            var updater = Force.PhysicsUpdate(
                                                      force,
                                                      stopRadius
                                                  );

            controller.RunAsyncFixed(
                () =>
                {
                    var isWithinRange = Vector3.Distance(target.transform.position, controller.transform.position) < radius;
                    Vector3 targetPosition = target.TryGetComponent(out Ragdoll ragdoll) ? ragdoll.neck.Get.position : target.transform.position;

                    if (isWithinRange)
                        updater(controller, targetPosition);

                },
                () => !controller.gameObject.activeInHierarchy
            );
            
        }

    }
}