using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Bullet;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class Ragdoll : SerializedMonoBehaviour
    {
        public Rigidbody[] limbs;
        public Animator animator;

        public Transform neck;

        public Tuple<Transform, Transform> hands = new Tuple<Transform, Transform>(null, null);

        public bool IsRagdolling { get; private set; } = false;
        
        private void Start()
        {
            // find all rigid bodies in children
            limbs = GetComponentsInChildren<Rigidbody>();
            
            // make them kinematic
            InitializeLimbs();

            if (neck == null)
            {
                neck = transform.FindRecursively(_ => _.name == "Neck");
            }
            
            if (hands.Left == null) hands.Set(new Left<Transform>(transform.FindRecursively(_ => _.name == "Hand_L")));

            if (!animator) animator = GetComponent<Animator>();

        }

        [Button]
        private void InitializeLimbs()
        {
            foreach (var limb in limbs)
            {
                limb.isKinematic = true;
                var l = limb.gameObject.GetOrElseAddComponent<Limb>();
                l.master = this;
            }
        }

        /// <summary>
        /// Sets the rigid body is kinematic to the given value.
        /// </summary>
        /// <param name="enabled">Sets it to be enabled.</param>
        /// <returns>The value that was set.</returns>
        public bool ToggleRagdoll(bool? enabled = null)
        {
            var shouldEnableRagdoll = enabled.HasValue ? enabled.Value : !IsRagdolling;
            if (animator)
                animator.enabled = !shouldEnableRagdoll; // animator is disabled when ragdoll is enabled
                

            foreach (var limb in limbs)
            {
                // is not kinematic when ragdoll is enabled
                limb.isKinematic = !shouldEnableRagdoll;
            }

            return IsRagdolling = shouldEnableRagdoll;
        }
        
        [Button] public void EnableRagdoll() { ToggleRagdoll(true); }
        [Button] public void DisableRagdoll() { ToggleRagdoll(false); }

        // limb tracks velocity to improve the ragdoll transition from an animation
        public class Limb : MonoBehaviour
        {
            public Ragdoll master;
         
            // todo: find a nicer way to control the multiplier
            public float damageMultiplier = 1f;

        }

        public void AttachImpactParticleSpawners(BulletImpactSettings settings)
        {
            // limbs have not been fetched yet, so we just initialize them.
            if (limbs == null || limbs.Length == 0) limbs = GetComponentsInChildren<Rigidbody>();
            
            foreach (var limb in limbs)
            {
                var spawner = limb.gameObject.AddComponent<ImpactParticleSpawner>();
                spawner.settings = settings;
            }
        }

    }
}