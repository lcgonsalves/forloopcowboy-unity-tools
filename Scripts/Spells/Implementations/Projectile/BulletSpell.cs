using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using VoxelArsenal;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile
{
    public class BulletSpell : Spell
    {
        [InlineEditor(InlineEditorModes.FullEditor)]
        public Bullet.Bullet bullet;

        [InlineEditor(InlineEditorModes.FullEditor)]
        public Transition handBackTransition;

        public bool enableHoverBulletPreview = true;

        public bool bulletUsesGravity = true;
        
        public float throwAngle = 0f;

        public float previewBulletRotation;

        public string BulletInstanceKey => $"{key} Bullet";
        
        public override void RegisterCustomParticles(InstanceConfiguration configuration)
        {
            configuration.RegisterCustom(BulletInstanceKey, bullet.prefab);
        }

        public override void Preview(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            base.Preview(caster, source, direction);
            
            if (
                caster.ParticleInstancesFor(this, source, out var particles) &&
                enableHoverBulletPreview && 
                particles.TryGetCustom(BulletInstanceKey, out var bulletPreview)
            )
            {
                var previewPosition = GetCastPointFor(source);
                
                bulletPreview.transform.Rotate(Vector3.up * ( previewBulletRotation * Time.deltaTime ));
                UpdateEffect(bulletPreview, previewPosition, bulletPreview.transform.rotation, "FirstPersonObjects");
                
            }
        }

        public override void ResetPreview(SpellUserBehaviour caster, Side<ArmComponent> source)
        {
            base.ResetPreview(caster, source);

            if (caster.ParticleInstancesFor(
                this,
                source,
                out var particles
            ) && particles.TryGetCustom(BulletInstanceKey, out var bulletPreview))
            {
                bulletPreview.SetActive(false);
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
            b.rb.useGravity = bulletUsesGravity;
            
            if (b.transform.childCount > 0)
                b.transform.GetChild(0).localScale = castScale * Vector3.one;
            
            OnBulletFired?.Invoke(caster, b);
        }

        public event Action<SpellUserBehaviour, BulletController> OnBulletFired;

        [MenuItem("Spells/New.../Bullet")]
        static void CreateBulletSpell(){ Spell.CreateSpellAsset<BulletSpell>("Projectile"); }

    }
}
