using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Weapon;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    /// Exposes methods for aiming at points
    public class AimComponent : MonoBehaviour
    {
        public Transition easeToAimTransition;
        public WeaponController weapon;

        [Tooltip("Transform to be rotated horizontally. By default uses transform of attached object. For a tank, use the parent object of the cannon.")]
        public Transform bodyTransform;
        
        [HideInInspector]
        public Transform weaponTransform => weapon.transform;

        [SerializeField, ReadOnly]
        private bool _isTracking = false;
        public bool isTracking { get => _isTracking; private set => _isTracking = value; }

        [SerializeField, ReadOnly]
        private Transform trackedTarget = null;

        protected void Update()
        {
            if (isTracking && (trackedTarget != null)) 
            { 
                Aim(trackedTarget.position, targetIsNew);
                targetIsNew = false;
            }
        }

        // Makes component execute Aim() on Update() loop, focusing on target's position.
        public void Track(Transform target)
        {
            targetIsNew = !trackedTarget || trackedTarget.GetInstanceID() != target.GetInstanceID();
            
            isTracking = true;
            trackedTarget = target;
        }

        public void StopTracking()
        {
            isTracking = false;
            trackedTarget = null;
            if ( aimTransition != null )
            {
                StopCoroutine(aimTransition);
                aimTransition = null;
            }
        }
        
        /// <summary>
        /// Rotates body transform and weapon transform to look at the target. If no body transform has been specified,
        /// body transform is set to this transform.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="gradual">When true, plays animation. If an animation is already playing, it interrupts it.</param>
        public void Aim(Vector3 target, bool gradual)
        {
            bodyTransform = bodyTransform != null ? bodyTransform : transform;

            // gradually aim towards target if it wasn't already
            if (gradual)
            {
                if (aimTransition != null) StopCoroutine(aimTransition);

                Quaternion initialWeaponRotation = weaponTransform.rotation;
                Quaternion initialBodyRotation = bodyTransform.rotation;
                
                aimTransition = easeToAimTransition.PlayOnce(
                    this,
                    state =>
                    {
                        // body just rotates on axis, so as to not tilt character
                        bodyTransform.rotation = Quaternion.Lerp(
                            initialBodyRotation,
                            Quaternion.LookRotation(new Vector3(target.x, bodyTransform.position.y, target.z) - bodyTransform.position),
                            state.Snapshot()
                        );

                        if (!weaponTransform) return; 
                        
                        // weapon aims directly at point
                        weaponTransform.rotation = Quaternion.Lerp(
                            initialWeaponRotation,
                            Quaternion.LookRotation(target - weaponTransform.position),
                            state.Snapshot()
                        );
                    },
                    finishState =>
                    {
                        bodyTransform.rotation = Quaternion.LookRotation(new Vector3(target.x, bodyTransform.position.y, target.z) - bodyTransform.position);
                        if (weaponTransform) weaponTransform.rotation = Quaternion.LookRotation(target - weaponTransform.position);
                    }
                );
            }
            else
            {
                bodyTransform.rotation = Quaternion.LookRotation(new Vector3(target.x, bodyTransform.position.y, target.z) - bodyTransform.position);
                if (weaponTransform) weaponTransform.rotation = Quaternion.LookRotation(target - weaponTransform.position);
            }
        }

        private Coroutine aimTransition = null;
        private bool targetIsNew = true;
        

    }
}
