using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Core
{
    public class Ragdoll : SerializedMonoBehaviour
    {
        public Rigidbody[] limbs;
        public Animator animator;
        public bool IsRagdolling { get; private set; } = false;
        
        private void Start()
        {
            // find all rigid bodies in children
            limbs = GetComponentsInChildren<Rigidbody>();
            
            // make them kinematic
            foreach (var limb in limbs)
            {
                limb.isKinematic = true;
                var l = limb.gameObject.GetOrElseAddComponent<Limb>();
                l.master = this;
            }

            if (!animator) animator = GetComponent<Animator>();
            
        }

        /// <summary>
        /// Sets the rigid body is kinematic to the given value.
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
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
            // todo: implement
            public Ragdoll master;
        }
        
    }
}