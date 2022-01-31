using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Weapon;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    /// Exposes methods for aiming at points
    public class AimComponent : SerializedMonoBehaviour
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

        [CanBeNull] public Transform TrackedTarget => trackedTarget;

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
        [Button("Track"), ButtonGroup("Aim")]
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
        [Button("Aim"), ButtonGroup("Aim")]
        public void Aim(Transform target, bool gradual, [CanBeNull] Action onAimReady = null)
        {
            if (weaponTransform == null) return;
            
            bodyTransform = bodyTransform != null ? bodyTransform : transform;
            
            void AimTowardsTarget(Vector3 targetPosition, Vector3 bodyPosition)
            {
                onAimReady?.Invoke();
                
                bodyTransform.rotation =
                    Quaternion.LookRotation(new Vector3(targetPosition.x, bodyPosition.y, targetPosition.z) - bodyPosition);
                if (weaponTransform) weaponTransform.rotation = Quaternion.LookRotation(targetPosition - weaponTransform.position);
            }

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
                            Quaternion.LookRotation(targetPosition - weaponTransform.position),
                            state.Snapshot()
                        );
                    },
                    finishState =>
                    {
                        if (target) AimTowardsTarget(target.position, bodyTransform.position);
                    }
                );
            }
            else AimTowardsTarget(target.position, bodyTransform.position);
        }

        private Coroutine aimTransition = null;

        /// <summary>
        /// Returns true if a raycast from the weapon's muzzle
        /// to the target global position hits one of the colliders
        /// from any of the spawned NPCs of the given side in the given
        /// unit manager.
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="side"></param>
        /// <param name="unitManager"></param>
        /// <returns></returns>
        public bool HasNPCInCrossfire(
            Vector3 targetPosition,
            UnitManager.Side side,
            UnitManager unitManager
        )
        {
            var possibleNPCs = unitManager.GetSpawned(side);
            List<Collider> possibleTargets = new List<Collider>(possibleNPCs.Length);

            foreach (var npc in possibleNPCs)
            {
                possibleTargets.AddRange(npc.GetComponents<Collider>());
                possibleTargets.AddRange(npc.GetComponentsInChildren<Collider>());
            }

            Transform muzzleSource = weapon ? weapon.muzzle : transform;

            var muzzleSourcePosition = muzzleSource.position;
            var hits = Physics.SphereCastAll(muzzleSourcePosition, 0.15f, targetPosition - muzzleSourcePosition);

            Debug.DrawRay(muzzleSourcePosition, targetPosition - muzzleSourcePosition);
            
            foreach (var raycastHit in hits)
            {
                if (possibleTargets.Contains(raycastHit.collider)) return true;
            }

            return false;
        }
        
    }
}
