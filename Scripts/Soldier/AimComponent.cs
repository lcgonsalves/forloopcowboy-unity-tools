using System;
using System.Collections;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Weapon;
using UnityEditor;
using UnityEngine;
using Action = System.Action;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    /// Exposes methods for aiming at points
    public class AimComponent : MonoBehaviour
    {
        public Soldier soldierSettings;

        [UnityEngine.Tooltip("Where the height is calculated from.")]
        public Transform eyeLevel;

        [UnityEngine.Tooltip("This means that the character can look up maximum 60 deg")]
        public Vector2 maxLookAngle = new Vector2(60, 50);
        public Vector2 minLookAngle = new Vector2(-60, -50);

        [HideInInspector, Obsolete]
        public string aimAnimationLayerName;

        public WeaponController weapon;
        
        [HideInInspector]
        public Transform weaponTransform => weapon.transform;

        [SerializeField, ReadOnly]
        private bool _isTracking = false;
        public bool isTracking { get => _isTracking; private set => _isTracking = value; }

        private Transform trackedTarget = null;

        private void Update()
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
            targetIsNew = trackedTarget.GetInstanceID() != target.GetInstanceID();
            
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
        /// Rotates transform and weapon transform to look at the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="gradual">When true, plays animation. If an animation is already playing, it interrupts it.</param>
        public void Aim(Vector3 target, bool gradual)
        {
            Transform bodyTransform = transform;

            // gradually aim towards target if it wasn't already
            if (gradual)
            {
                if (aimTransition != null) StopCoroutine(aimTransition);

                Quaternion initialWeaponRotation = weaponTransform.rotation;
                Quaternion initialBodyRotation = bodyTransform.rotation;
                
                aimTransition = soldierSettings.easeToAimTransition.PlayOnce(
                    this,
                    state =>
                    {
                        // body just rotates on axis, so as to not tilt character
                        bodyTransform.rotation = Quaternion.Lerp(
                            initialBodyRotation,
                            Quaternion.LookRotation(new Vector3(target.x, bodyTransform.position.y, target.z) - bodyTransform.position),
                            state.Snapshot()
                        );
                        
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
                        weaponTransform.rotation = Quaternion.LookRotation(target - weaponTransform.position);
                    }
                );
            }
            else
            {
                bodyTransform.rotation = Quaternion.LookRotation(new Vector3(target.x, bodyTransform.position.y, target.z) - bodyTransform.position);
                weaponTransform.rotation = Quaternion.LookRotation(target - weaponTransform.position);
            }
        }

        private Coroutine aimTransition = null;
        private bool targetIsNew = true;
        

    }
}
