using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Weapon;
using JetBrains.Annotations;
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
        
        [HideInInspector] [CanBeNull] public Transform weaponTransform => weapon ? weapon.transform : null;

        [SerializeField, ReadOnly]
        private bool _isTracking = false;
        public bool isTracking { get => _isTracking; private set => _isTracking = value; }

        [SerializeField, ReadOnly]
        private Transform trackedTarget = null;

        protected void Update()
        {
            if (isTracking && (trackedTarget != null)) 
            { 
                Aim(trackedTarget, false);
            }
        }

        // Makes component execute Aim() on Update() loop, focusing on target's position.
        public void Track(Transform target)
        {
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
        /// Gradually aims towards target, then tracks it.
        /// If given target is the same as the tracked target, nothing happens.
        /// </summary>
        /// <param name="target"></param>
        public void AimAndTrack(Transform target)
        {
            if (trackedTarget == null || trackedTarget.GetInstanceID() != target.GetInstanceID())
            {
                Aim(target, true, () =>
                {
                    Track(target);
                });
            }
        }

        /// <summary>
        /// Rotates body transform and weapon transform to look at the target. If no body transform has been specified,
        /// body transform is set to this transform.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="gradual">When true, plays animation. If an animation is already playing, it interrupts it.</param>
        /// <param name="onAimReady">Callback that runs when aim is ready</param>
        public void Aim(Transform target, bool gradual, [CanBeNull] Action onAimReady = null)
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
                        var targetPosition = target.position;
                        var bodyPosition = bodyTransform.position;
                        
                        bodyTransform.rotation = Quaternion.Lerp(
                            initialBodyRotation,
                            Quaternion.LookRotation(new Vector3(targetPosition.x, bodyPosition.y, targetPosition.z) - bodyPosition),
                            state.Snapshot()
                        );

                        if (!weaponTransform) return; 
                        
                        // weapon aims directly at point
                        weaponTransform.rotation = Quaternion.Lerp(
                            initialWeaponRotation,
                            Quaternion.LookRotation(target.position - weaponTransform.position),
                            state.Snapshot()
                        );
                    },
                    finishState =>
                    {
                        var targetPosition = target.position;
                        var bodyPosition = bodyTransform.position;
                        
                        onAimReady?.Invoke();
                        bodyTransform.rotation = Quaternion.LookRotation(new Vector3(targetPosition.x, bodyPosition.y, targetPosition.z) - bodyPosition);
                        if (weaponTransform) weaponTransform.rotation = Quaternion.LookRotation(target.position - weaponTransform.position);
                    }
                );
            }
            else
            {
                var targetPosition = target.position;
                var bodyPosition = bodyTransform.position;
                
                onAimReady?.Invoke();
                bodyTransform.rotation = Quaternion.LookRotation(new Vector3(targetPosition.x, bodyPosition.y, targetPosition.z) - bodyPosition);
                if (weaponTransform) weaponTransform.rotation = Quaternion.LookRotation(targetPosition - weaponTransform.position);
            }
        }

        private Coroutine aimTransition = null;
        

    }
}
