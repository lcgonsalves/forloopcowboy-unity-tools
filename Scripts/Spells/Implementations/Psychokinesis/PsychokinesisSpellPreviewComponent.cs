using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using JetBrains.Annotations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Misc
{
    [RequireComponent(typeof(SphereCollider))]
    public class PsychokinesisSpellPreviewComponent : MonoBehaviour
    {
        private LazyGetter<SphereCollider> triggerGetter = new LazyGetter<SphereCollider>();

        [CanBeNull] public PsychokinesisSpell spell;

        public Func<Collider, Vector3, bool> PhysicsUpdater => 
            spell ? Force.PhysicsUpdate(spell.forceSettings) : ((c, v) => false);

        public SphereCollider ThisTrigger => triggerGetter.Get(gameObject);

        [SerializeField, Tooltip("This setting is overriden by the spell setting.")]
        private bool _useGravity = false;
        
        public bool useGravity => spell ? spell.useGravity : _useGravity;
        
        /// <summary>
        /// Center of attraction, based on the trigger position relative to this
        /// instance.
        /// </summary>
        public Vector3 PivotPoint => transform.TransformPoint(ThisTrigger.center);

        /// <summary>
        /// By default, bullets are valid targets.
        /// </summary>
        public virtual Func<Collider, bool> isValid => c => c.HasComponent<BulletController>();


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
            if (!isValid(other)) return;
            
            if (other.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = useGravity;
                rb.velocity = Vector3.zero;
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
            if (!isValid(other)) return;

            PhysicsUpdater(other, PivotPoint);
        }

        /// <summary>
        /// Once the object leaves the trigger area,
        /// physics are restored to the object.
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerExit(Collider other)
        {
            if (!isValid(other)) return;
            
            if (other.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = true;
            }
            
            objectsInRange.Remove(other);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            if (ThisTrigger) Gizmos.DrawWireSphere(transform.TransformPoint(ThisTrigger.center), ThisTrigger.radius);
            
            Gizmos.color = Color.red;
            if (spell) Gizmos.DrawWireSphere(transform.TransformPoint(ThisTrigger.center), spell.forceSettings.stopRadius);
        }
    }
}