using System;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Weapon;
using JetBrains.Annotations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [RequireComponent(typeof(Animator))]
    public class AimComponentWithIK : AimComponent
    {
        public List<WeaponIKSettings> supportHandIKSettings = new List<WeaponIKSettings>();
        
        protected Animator _animator;
        protected Dictionary<int, WeaponIKSettings> settingsForWeapon = new Dictionary<int, WeaponIKSettings>();

        [SerializeField] public Transition ikLerpIn;
        [SerializeField] public Transition ikLerpOut;

        // [Serializable]
        // public struct AnimatorIntegrationSettings
        // {
        //
        //     [Serializable]
        //     public class IKFromAnimatorState
        //     {
        //         public enum Boolean { True, False }
        //         
        //         [SerializeField] private WeaponController enableIKFor;
        //         [SerializeField] private string whenAnimatorParam;
        //         [SerializeField] private Boolean isFoundToBe;
        //         [SerializeField] private float setIKWeightToBe;
        //
        //         public WeaponController targetWeapon => enableIKFor;
        //         public string animatorParam => whenAnimatorParam;
        //         public bool value => isFoundToBe == Boolean.True;
        //         public float targetWeight => setIKWeightToBe;
        //     }
        //     
        //     public void Enable() { enabled = true; }
        //     public void Disable() { enabled = false; }
        //     public bool enabled;
        //     public List<IKFromAnimatorState> setIKFromAnimatorParam;
        // }
        //
        // public AnimatorIntegrationSettings animatorSettings;
        
        private void OnEnable()
        {
            Initialize();
        }

        internal void Initialize()
        {
            _animator = GetComponent<Animator>();

            settingsForWeapon.Clear();

            // cache dictionary for easier access
            foreach (var setting in supportHandIKSettings)
            {
                // only include if both the target and the weapon are defined
                if (setting.forWeapon != null && setting.target != null)
                    settingsForWeapon.Add(setting.forWeapon.GetInstanceID(), setting);
                else
                    Debug.LogWarning(
                        $"[{gameObject.name}] Setting does not have either weapon or IK target assigned, will not be considered.");
            }
        }

        private Coroutine ikLerp;

        /// <summary>
        /// Lerps the weight on the current IK settings to zero, switches weapon to selected weapon,
        /// then lerps the weight back to 1.
        /// </summary>
        /// <param name="newWeapon"></param>
        /// <param name="onEnd">Runs when ik weight finishes lerping.</param>
        public void Select(WeaponController newWeapon, Action onEnd)
        {
            // always reset lerp
            // always interrup previous lerps
            if (ikLerp != null)
            {
                StopCoroutine(ikLerp);
                ikLerp = null;
            }
            
            // first lerp out
            ikLerp = LerpIKOutFor(weapon, () =>
            {
                // when done, set weapon
                weapon = newWeapon;
                
                // now lerp in with new weapon
                ikLerp = LerpIKInFor(newWeapon, onEnd);

            });
        }

        private Coroutine LerpIKInFor(WeaponController newWeapon, Action onEnd)
        {
            return LerpIKFor(newWeapon, 1f, onEnd);
        }
        
        private Coroutine LerpIKOutFor(WeaponController newWeapon, Action onEnd)
        {
            return LerpIKFor(newWeapon, 0f, onEnd);
        }

        [CanBeNull] private WeaponIKSettings _cachedCurrentIKSettings;
        [CanBeNull]
        public WeaponIKSettings currentIKSettings
        {
            get
            {
                if (weapon && _cachedCurrentIKSettings != null && _cachedCurrentIKSettings.forWeapon.GetInstanceID() == weapon.GetInstanceID())
                {
                    return _cachedCurrentIKSettings;
                }

                if (weapon && settingsForWeapon.TryGetValue(weapon.GetInstanceID(), out var settings))
                {
                    _cachedCurrentIKSettings = settings;   
                    return settings;
                }

                return null;
            }
        }

        public bool TryGetSettings(WeaponController wpn, out WeaponIKSettings settings)
        {
            if (settingsForWeapon.TryGetValue(weapon.GetInstanceID(), out var s))
            {
                settings = s;
                return true;
            }

            settings = (WeaponIKSettings) null;
            return false;
        }

        /// <summary>
        /// Lerps ik weights for target weapon, both to given weight.
        /// When lerp is done, calls onEnd;
        /// If unable to perform lerp, onEnd is called immediately.
        /// </summary>
        /// <param name="targetWeapon"></param>
        /// <param name="weight"></param>
        /// <param name="onEnd"></param>
        public Coroutine LerpIKFor(WeaponController targetWeapon, float weight, Action onEnd, float? overrideDuration = null)
        {
            void update(WeaponIKSettings weaponIKSettings,  Transition.TransitionState state, float initialTWeight, float initialRWeight)
            {
                weaponIKSettings.translation.SetWeight(Mathf.Lerp(initialTWeight, weight, state.Snapshot()));
                weaponIKSettings.rotation.SetWeight(Mathf.Lerp(initialRWeight, weight, state.Snapshot()));
            }

            float TOLERANCE = 0.05f;

            // lerp current ik to target weight as long 
            if (
                targetWeapon &&
                settingsForWeapon.TryGetValue(targetWeapon.GetInstanceID(), out var curWeaponIK) &&
                ((Math.Abs(curWeaponIK.translation.weight - weight) > TOLERANCE || 
                  Math.Abs(curWeaponIK.rotation.weight - weight) > TOLERANCE)) &&
                ikLerpOut != null // no transition == cuts straight to next ik point
            )
            {
                float initialTWeight = curWeaponIK.translation.weight;
                float initialRWeight = curWeaponIK.rotation.weight;

                return ikLerpOut.PlayOnceWithDuration(
                    this,
                    state => { update(curWeaponIK, state, initialTWeight, initialRWeight); },
                    endState =>
                    {
                        // set it once more
                        update(curWeaponIK, endState, initialTWeight, initialRWeight);

                        // callback
                        onEnd();
                    },
                    (overrideDuration.HasValue ? overrideDuration.Value : ikLerpOut.duration) *
                    Mathf.Max(Mathf.Abs(initialTWeight - weight), Mathf.Abs(initialRWeight - weight)) // scale duration to match initial weight
                );
            }
            else onEnd();
            return (Coroutine) null;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (weapon != null && settingsForWeapon.TryGetValue(weapon.GetInstanceID(), out var ikSettings))
            {
                _animator.SetIKPosition(ikSettings.limb, ikSettings.target.TransformPoint(ikSettings.translation.value));
                var trgtRotation = ikSettings.target.rotation.eulerAngles;
                
                _animator.SetIKRotation(
                    ikSettings.limb,
                    Quaternion.Euler(
                        trgtRotation.x + ikSettings.rotation.value.x,
                        trgtRotation.y + ikSettings.rotation.value.y,
                        trgtRotation.z + ikSettings.rotation.value.z
                    )
                );
                
                _animator.SetIKPositionWeight(ikSettings.limb, ikSettings.translation.weight);
                _animator.SetIKRotationWeight(ikSettings.limb, ikSettings.rotation.weight);
            }
        }
    }

    [Serializable]
    public class WeaponIKSettings : SimpleIKSettings
    {
        public WeaponController forWeapon;

        private static IKWeightSettings<Vector3> defaultSettings => new IKWeightSettings<Vector3>();

        public new string path => target.GetPathFrom(forWeapon.transform.name);

        public WeaponIKSettings() : base(defaultSettings, defaultSettings)
        {
            this._limb = AvatarIKGoal.LeftHand;
        }
    }
}