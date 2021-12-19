using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile
{
    public class BulletSpell : Spell
    {
        [Header("Replaces Main Effect if none is specified"), InlineEditor(InlineEditorModes.FullEditor)]
        public Bullet.Bullet bullet;

        [InlineEditor(InlineEditorModes.FullEditor)]
        public Transition handBackTransition;

        public float throwAngle = 0f;

        protected Dictionary<int, BulletController> hoveringBullets = new Dictionary<int, BulletController>();

        public override void Preview(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            if (!CanCast(caster, source)) return;
    
            PrepareBulletCache(caster);

            bool hasParticleInstances = caster.ParticleInstancesFor(this, source, out var instances);

            // spin and hover bullets if no preview particle instance is defined
            if (hasParticleInstances && instances.preview != null)
            {
                base.Preview(caster, source, direction);
            }
            
            if (hoveringBullets.TryGetValue(source.content.GetInstanceID(), out BulletController sphere))
            {
                sphere.gameObject.SetActive(true);
                
                var castPoint = source.content.GetCastPoint(chargeStyle);

                sphere.rb.MovePosition(castPoint);
                
                sphere.rb.useGravity = false;
                sphere.rb.AddTorque(0.001f, 0.02f, 0f);
                sphere.rb.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FirstPersonObjects"));
                var t = sphere.rb.transform;

                if (t.childCount > 0)
                {
                    t.GetChild(0).localScale = previewScale * Vector3.one;
                }
            }

        }

        private void PrepareBulletCache(SpellUserBehaviour caster)
        {

            if (!hoveringBullets.ContainsKey(caster.leftArm.GetInstanceID()) || !hoveringBullets.ContainsKey(caster.rightArm.GetInstanceID()))
            {
                var l = GameObject.Instantiate(bullet.prefab).gameObject.GetOrElseAddComponent<BulletController>();
                var r = GameObject.Instantiate(bullet.prefab).gameObject.GetOrElseAddComponent<BulletController>();

                l.Settings = bullet;
                r.Settings = bullet;

                if (l is EnergyBulletController)
                {
                    EnergyBulletController energyBullet = (EnergyBulletController) l;
                    energyBullet.dieOnImpact = false; // preview bullet shouldn't interact with the environment

                    energyBullet = (EnergyBulletController) r;
                    energyBullet.dieOnImpact = false; // preview bullet shouldn't interact with the environment
                    // energyBullet.rb.isKinematic = true;
                    l.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                    r.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                }

                hoveringBullets.Add(caster.leftArm.GetInstanceID(), l);
                hoveringBullets.Add(caster.rightArm.GetInstanceID(), r);
            }
        }

        public override void ResetPreview(SpellUserBehaviour caster, Side<ArmComponent> arm)
        {
            PrepareBulletCache(caster);

            bool l = caster.ParticleInstancesFor(this, caster.arms.l, out var instancesL);
            bool r = caster.ParticleInstancesFor(this, caster.arms.r, out var instancesR);

            bool hasParticleInstances = l || r;

            // spin and hover bullets if no preview particle instance is defined
            if (hasParticleInstances)
            {
                if (arm is Left<ArmComponent>) instancesL.preview?.gameObject.SetActive(false);
                if (arm is Right<ArmComponent>) instancesR.preview?.gameObject.SetActive(false);
            }

            if (hoveringBullets.TryGetValue(arm.content.GetInstanceID(), out BulletController sphere))
            {
                sphere.gameObject.SetActive(false);
            }
        }

        protected override void Execute(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            // direction is from screen. we project a point forward in the range, then find the 
            // direction from the hand to said projected point to correct for this.
            var castPoint = GetCastPointFor(source);
            var projectedPoint = caster.mainCamera.transform.position + (direction.normalized * range);
        
            // override projected point with raycast hit for better precision if within range
            if (Physics.Raycast(new Ray(caster.mainCamera.transform.position, direction), out var hit, range))
                projectedPoint = hit.point;
            
            var correctedDirection = Quaternion.AngleAxis(throwAngle, caster.transform.TransformDirection(Vector3.left)) * (projectedPoint - castPoint).normalized;

            var b = caster.gameObject.GetOrElseAddComponent<BulletSystem>().SpawnAndFire(bullet, castPoint, correctedDirection);
            b.rb.AddTorque(5f, 3f, 0f);
            b.rb.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Player"));

            if (b.transform.childCount > 0)
                b.transform.GetChild(0).localScale = castScale * Vector3.one;
            
            OnBulletFired?.Invoke(caster, b);

            if (hoveringBullets.TryGetValue(source.content.GetInstanceID(), out BulletController sphere))
            {
                sphere.rb.angularVelocity = Vector3.zero; // reset spin
            } else PrepareBulletCache(caster);
        }

        public event Action<SpellUserBehaviour, BulletController> OnBulletFired;

        [MenuItem("Spells/New.../Bullet")]
        static void CreateBulletSpell(){ Spell.CreateSpell<BulletSpell>("Projectile"); }

    }
}
