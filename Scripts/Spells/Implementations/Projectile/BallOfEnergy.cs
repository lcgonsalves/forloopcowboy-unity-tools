using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Player;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile
{
    public class BallOfEnergy : Spell
    {
        [Header("Replaces Main Effect if none is specified")]
        public Bullet.Bullet bullet;

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
                instances.preview.gameObject.SetActive(true);
                instances.preview.transform.position = source.content.GetCastPoint(chargeStyle);
            }
            else if (hoveringBullets.TryGetValue(source.content.GetInstanceID(), out BulletController sphere))
            {
                sphere.gameObject.SetActive(true);
                sphere.rb.MovePosition(source.content.GetCastPoint(chargeStyle));
                sphere.rb.useGravity = false;
                sphere.rb.AddTorque(0.001f, 0.02f, 0f);
            }

        }

        private void PrepareBulletCache(SpellUserBehaviour caster)
        {

            if (!hoveringBullets.ContainsKey(caster.leftArm.GetInstanceID()) || !hoveringBullets.ContainsKey(caster.rightArm.GetInstanceID()))
            {
                var l = GameObject.Instantiate(bullet.prefab).gameObject.GetOrElseAddComponent<BulletController>();
                var r = GameObject.Instantiate(bullet.prefab).gameObject.GetOrElseAddComponent<BulletController>();
            
                l.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FPS"));
                r.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FPS"));
            
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

        public override void Reset(SpellUserBehaviour caster, Side<ArmComponent> arm)
        {
            PrepareBulletCache(caster);

            bool l = caster.ParticleInstancesFor(this, caster.arms.l, out var instancesL);
            bool r = caster.ParticleInstancesFor(this, caster.arms.r, out var instancesR);

            bool hasParticleInstances = l || r;

            // spin and hover bullets if no preview particle instance is defined
            if (hasParticleInstances)
            {
                instancesL.preview?.gameObject.SetActive(false);
                instancesR.preview?.gameObject.SetActive(false);
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
            
            var correctedDirection = Quaternion.AngleAxis(throwAngle, Vector3.left) * (projectedPoint - castPoint).normalized;

            var b = BulletSystem.SpawnAndFire(bullet, castPoint, correctedDirection);
            b.rb.AddTorque(5f, 3f, 0f);

            if (hoveringBullets.TryGetValue(source.content.GetInstanceID(), out BulletController sphere))
            {
                sphere.rb.angularVelocity = Vector3.zero; // reset spin
            } else PrepareBulletCache(caster);
        }

        [MenuItem("Spells/New.../Projectile")]
        static void CreateBulletSpell(){ Spell.CreateSpell<BallOfEnergy>("Projectile"); }

    }
}
