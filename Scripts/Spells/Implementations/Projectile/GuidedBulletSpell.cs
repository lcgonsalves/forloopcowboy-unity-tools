using System;
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
    public class GuidedBulletSpell : BulletSpell
    {

        public float Radius;
        public float StopRadius;
        public float Force;
        
        private void OnEnable()
        {
            OnBulletFired += OnBulletFiredHandler;
        }

        private void OnDisable()
        {
            OnBulletFired -= OnBulletFiredHandler;
        }

        public override void Preview(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            base.Preview(caster, source, direction);

            if (caster.target != null && caster.ParticleInstancesFor(this, source, out var particles))
            {
                particles.preview.transform.position = caster.target.position;
            }
            
        }

        private void OnBulletFiredHandler(SpellUserBehaviour caster, BulletController controller)
        {
            // no target, no guide
            if (caster.target == null) return;

            var force = controller.GetOrElseAddComponent<Force>();

            force.enabled = true;
            
            force.m_Force = Force;
            force.m_Pivot = caster.target;
            force.m_Radius = Radius;
            force.m_StopRadius = StopRadius;
            force.m_Layers = LayerHelper.LayerMaskFor(controller.gameObject.layer);
            force.m_Type = GameLogic.Force.ForceType.Attraction;

        }
        
        [MenuItem("Spells/New.../GuidedBullet")]
        static void CreateBulletSpell(){ Spell.CreateSpell<GuidedBulletSpell>("Guided Projectile"); }
    }
}