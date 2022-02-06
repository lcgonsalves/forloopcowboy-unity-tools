using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Misc
{
    public class PsychokinesisSpellPreviewComponent : SerializedMonoBehaviour
    {

        [OnValueChanged("UpdateCoreColliderRadius")]
        [CanBeNull] public PsychokinesisDeprecatedSpell spell;

        /// <summary>
        /// The entity who is in charge of this spell preview.
        /// Knowing who the caster is prevents this spell from affecting
        /// spells cast by the same entity.
        /// </summary>
        [CanBeNull] public SpellUserBehaviour caster = null;
        
        public Func<Collider, Vector3, bool> PhysicsUpdater => 
            spell ? Force.PhysicsUpdate(spell.forceSettings) : ((c, v) => false);
        
        /// <summary>
        /// Used to detect if an object is in range for attraction.
        /// </summary>
        public SphereCollider trigger;
        
        /// <summary>
        /// Where objects are attracted to.
        /// </summary>
        [OnValueChanged("UpdateCoreColliderRadius")]
        public SphereCollider coreCollider;

        [SerializeField, Tooltip("This setting is overriden by the spell setting.")]
        private bool _useGravity = false;
        
        public bool useGravity => spell ? spell.useGravity : _useGravity;
        
        /// <summary>
        /// Center of attraction, based on the trigger position relative to this
        /// instance.
        /// </summary>
        public Vector3 PivotPoint => trigger ? transform.TransformPoint(trigger.center) : transform.position;

        /// <summary>
        /// By default, bullets are valid targets, as long as they haven't
        /// been fired by the caster themself.
        /// </summary>
        public static bool IsValid(Collider c, SpellUserBehaviour caster) => 
            c.TryGetComponent(out BulletController bc) && (bc.firedBy == null || bc.firedBy.GetInstanceID() != caster.gameObject.GetInstanceID());


        /// <summary>
        /// All objects who have entered and not left
        /// the trigger area are contained in this collection.
        /// </summary>
        public HashSet<Collider> objectsInRange = new HashSet<Collider>();

        /// <summary>
        /// New object entered the trigger. If a valid
        /// target, then we stop its physics.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!caster || !IsValid(other, caster)) return;
            
            if (other.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = useGravity;
                rb.velocity = Vector3.zero;
            }
            
            // if it is a bullet, stop counting bounces so it doesn't accidentally get disabled while within our grasp
            if (other.TryGetComponent(out BulletController bc))
            {
                bc.countBounces = false;
            }

            objectsInRange.Add(other);

            PhysicsUpdater(other, PivotPoint);
        }

        /// <summary>
        /// While object is within the trigger, if a valid object,
        /// then it is pulled towards the focus point.
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (!caster || !IsValid(other, caster)) return;

            PhysicsUpdater(other, PivotPoint);
        }

        /// <summary>
        /// Once the object leaves the trigger area,
        /// physics are restored to the object.
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerExit(Collider other)
        {
            if (!caster || !IsValid(other, caster)) return;

            OnObjectExitCleanup(other);

        }

        
        /// <summary>
        /// To be executed on any objects that leave the
        /// trigger. Resets the rigidbody / bullet to the
        /// state it was before this object came in contact
        /// with the spell.
        /// </summary>
        /// <param name="other"></param>
        public OnObjectExitCleanupResult OnObjectExitCleanup(Collider other)
        {
            if (other.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = true;
            }

            if (other.TryGetComponent(out BulletController bc))
            {
                bc.countBounces = true;
            }
            
            objectsInRange.Remove(other);

            return new OnObjectExitCleanupResult(
                rb,
                bc
            );
        }

        public struct OnObjectExitCleanupResult
        {
            [CanBeNull] public Rigidbody rb;
            [CanBeNull] public BulletController bulletController;

            public OnObjectExitCleanupResult([CanBeNull] Rigidbody rb, [CanBeNull] BulletController bulletController)
            {
                this.rb = rb;
                this.bulletController = bulletController;
            }

            #nullable enable
            public void Deconstruct(out Rigidbody? rb, out BulletController? bc)
            {
                rb = this.rb;
                bc = this.bulletController;
            }
            
            #nullable disable
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            if (trigger) Gizmos.DrawWireSphere(transform.TransformPoint(trigger.center), trigger.radius);
            
            Gizmos.color = Color.red;
            if (spell) Gizmos.DrawWireSphere(transform.TransformPoint(trigger.center), spell.forceSettings.stopRadius);
        }

        private void UpdateCoreColliderRadius()
        {
            if (coreCollider)
            {
                if (spell)
                {
                    coreCollider.radius = spell.forceSettings.stopRadius;
                }
            }
            else throw new NullReferenceException("Cannot update core collider radius as core collider is null.");
        }
    }
}