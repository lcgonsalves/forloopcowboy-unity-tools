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
                particles.preview.transform.localScale = Vector3.one;
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
            
            var collider = controller.GetComponentInChildren<Collider>();
            if (force.m_AffectedObjectIDs == null)
                force.m_AffectedObjectIDs = new HashSet<int> {collider.GetInstanceID()};
            else force.m_AffectedObjectIDs.Add(collider.GetInstanceID());
        }
        
        [MenuItem("Spells/New.../GuidedBullet")]
        static void CreateBulletSpell(){ Spell.CreateSpell<GuidedBulletSpell>("Guided Projectile"); }
    }
}