#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Weapon;
using JetBrains.Annotations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [ExecuteInEditMode, SelectionBase]
    public class WeaponUser : MonoBehaviour
    {
        [Tooltip("Where weapon is to be placed when in hand.")]
        public Transform triggerHandTransform;

        [Tooltip("Which hand will hold the magazine.")]
        public Transform reloadHandTransform;

        public AimComponent aimComponent;

        public enum WeaponType
        {
            Primary,
            Secondary
        }

        public interface WeaponContainer
        {
            [CanBeNull] Transform weaponTransform { get; }
            Vector3 correctiveRotation { get; set; }
            Vector3 correctiveTranslation { get; set; }
        }

        [Serializable]
        public struct AnimatorIntegrationSettings
        {
            [Serializable]
            public struct WeaponTypeAnimParams
            {
                public WeaponType weaponType;
                public string animParamName;

                public WeaponTypeAnimParams(WeaponType weaponType) : this()
                {
                    this.weaponType = weaponType;
                    animParamName = weaponType.ToString();
                }

                public WeaponTypeAnimParams(WeaponType weaponType, string animParamName) : this()
                {
                    this.weaponType = weaponType;
                    this.animParamName = animParamName;
                }
            }

            public void Enable()
            {
                enabled = true;
            }

            public void Disable()
            {
                enabled = false;
            }

            public bool enabled;

            [Tooltip(
                "Whenever a weapon of said type is selected, the component will set a bool of the defined string.")]
            public List<WeaponTypeAnimParams> animatorParameters;

            public AnimatorIntegrationSettings(bool enabled)
            {
                this.enabled = enabled;
                animatorParameters = new List<WeaponTypeAnimParams>();
            }

            /// <summary>
            /// Applies all of the defined parameters to the
            /// animator given the active weapon item.
            /// </summary>
            /// <param name="animator"></param>
            /// <param name="activeWeapon"></param>
            public void ApplyParameters(Animator animator, WeaponItem? activeWeapon)
            {
                // if null, just set everything to false
                if (activeWeapon == null)
                {
                    foreach (var animatorParameter in animatorParameters)
                    {
                        animator.SetBool(animatorParameter.animParamName, false);
                    }
                }

                // if weapon is selected, set everything to false except those of type, which are true
                else
                {
                    var type = activeWeapon.type;
                    foreach (var animatorParameter in animatorParameters)
                    {
                        animator.SetBool(animatorParameter.animParamName, animatorParameter.weaponType == type);
                    }
                }
            }

        }

        [SerializeField] public AnimatorIntegrationSettings animatorSettings;
        private Animator _animator;

        /// <summary>
        /// Saves local rotation and position
        /// of weapon transforms in container.
        /// </summary>
        /// <param name="container"></param>
        public static void GetTransformsFromWeapon(WeaponContainer container)
        {
            if (container.weaponTransform is { })
            {
                container.correctiveRotation = container.weaponTransform.localRotation.eulerAngles;
                container.correctiveTranslation = container.weaponTransform.localPosition;
            }
        }

        /// <summary>
        /// Applies corrective rotation and translation to
        /// the weapon inside the container, if any.
        /// </summary>
        /// <param name="container"></param>
        public static void ApplyTransformationsToWeapon(WeaponContainer container)
        {
            if (container?.weaponTransform is null) return;

            container.weaponTransform.localRotation = Quaternion.Euler(container.correctiveRotation);
            container.weaponTransform.localPosition = container.correctiveTranslation;
        }

        [Serializable]
        public class WeaponItem : WeaponContainer
        {
            public WeaponController? weapon;
            public WeaponType type;

            [SerializeField, Tooltip("Only applies when weapon is in hand.")]
            private Vector3 _correctiveRotation;

            [SerializeField, Tooltip("Only applies when weapon is in hand.")]
            private Vector3 _correctiveTranslation;

            public WeaponItem(WeaponController weapon, WeaponType type)
            {
                this.weapon = weapon;
                this.type = type;
            }

            public WeaponItem()
            {
                _correctiveRotation = Vector3.zero;
                _correctiveTranslation = Vector3.zero;
            }

            /// <summary>
            /// Equality is on the basis of the underlying weapon instance ID.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                if (obj is WeaponItem w)
                {
                    if (w.weapon == null || weapon == null) return w.weapon == weapon;

                    return weapon.transform.GetInstanceID() == w.weapon.transform.GetInstanceID();
                }

                return false;
            }

            [CanBeNull] public Transform weaponTransform => weapon?.transform;

            public Vector3 correctiveRotation
            {
                get => _correctiveRotation;
                set => _correctiveRotation = value;
            }

            public Vector3 correctiveTranslation
            {
                get => _correctiveTranslation;
                set => _correctiveTranslation = value;
            }

            public override string ToString()
            {
                return (weapon != null ? weapon.transform.name : "None") + $" [{type.ToString()}]";
            }
        }

        /// <summary>
        /// Represents an association between a weapon and a holsterOrWeapon. Not the state, meaning
        /// that the content is not representative of the state of an individuial holsterOrWeapon, but
        /// whether there is a weapon item that should be placed there when not active.
        /// </summary>
        [Serializable]
        public class WeaponHolster : WeaponContainer
        {
            public WeaponType type;

            public WeaponItem? content
            {
                get
                {
                    // if there's no weapon, then there's no weapon item.
                    if (_content.weapon == null) return null;

                    return _content;
                }
                set
                {
                    if (value != null)
                    {
                        _content = (WeaponItem) value;
                    }
                }
            }

            [SerializeField] private WeaponItem _content;

            [Tooltip("Where weapon is placed when NOT in hand.")]
            public Transform holsterTransform;

            [SerializeField, Tooltip("Only applies to weapon when weapon is in holsterOrWeapon.")]
            private Vector3 _correctiveRotation;

            [SerializeField, Tooltip("Only applies to weapon when weapon is in holsterOrWeapon.")]
            private Vector3 _correctiveTranslation;

            public WeaponHolster(Transform holsterTransform, WeaponType type, WeaponItem? item)
            {
                this.holsterTransform = holsterTransform;
                this.type = type;
                this.content = item;
            }

            public WeaponHolster()
            {
            }

            public bool Contains(WeaponItem item)
            {
                return content?.Equals(item) ?? false;
            }

            // intellij did this, i trust it
            [CanBeNull]
            public Transform weaponTransform => content?.weaponTransform != null
                ? content.weaponTransform != null ? content.weaponTransform.transform : null
                : null;

            public Vector3 correctiveRotation
            {
                get => _correctiveRotation;
                set => _correctiveRotation = value;
            }

            public Vector3 correctiveTranslation
            {
                get => _correctiveTranslation;
                set => _correctiveTranslation = value;
            }

            public override string ToString()
            {
                return content?.weapon.transform.name.ToString() + " " + type.ToString();
            }
        }

        [SerializeField,
         Tooltip(
             "Where the character can store weapons. If character has any weapons in inventory that don'to have a valid holsterOrWeapon (of same type) they will be dropped.")]
        public List<WeaponHolster> holsters = new List<WeaponHolster>();

        [SerializeField,
         Tooltip(
             "List of weapons in inventory. If character has any weapons in inventory that don'to have a valid holsterOrWeapon (of same type) they will be dropped.")]
        public List<WeaponItem> inventory = new List<WeaponItem>();

        private WeaponItem? _active = null;
        private static readonly int Reload = Animator.StringToHash("Reload");
        private static readonly int SwitchWeapon = Animator.StringToHash("SwitchWeapon");

        /// <summary>
        /// Setting the active weapon type to a different type
        /// will pick the first item with the given type and
        /// set it to active, and then equip it while holstering the other item.
        /// </summary>
        public WeaponType Active
        {
            get => _active?.type ?? WeaponType.Primary;
            set
            {
                if (_active?.type != value)
                {
                    HolsterActive();
                    EquipWeapon(value);
                }
            }
        }

        /// <summary>
        /// Fires every time a new weapon is equipped, an old weapon is unequipped.
        /// </summary>
        public event Action<WeaponItem?> onWeaponChanged;

        public void OpenFire()
        {
            _active?.weapon?.OpenFire(true);
        }

        public void CeaseFire()
        {
            _active?.weapon?.CeaseFire();
        }

        public void EquipActive()
        {
            EquipWeapon(Active);
        }

        public void EquipNext()
        {
            var types = EnumUtil.GetValues<WeaponType>().ToArray();
            int indexOfCurrent = -1;

            int i = 0;
            foreach (var weaponType in types)
            {
                if (weaponType == previousWeaponTypeActive)
                {
                    indexOfCurrent = i;
                    break;
                }

                i++;
            }

            if (indexOfCurrent < 0) throw new Exception("Active weapon type not part of weapon type? WTF");

            // warning here: not redundant because we're initializing at -1, but we want to get whatever value is set in the iteration before. read the code idiot (aka ME)
            var nextIndex = (indexOfCurrent + 1) % types.Length;
            Active = types[nextIndex];

        }

        /// <summary>
        /// Equips first holstered weapon of the given type.
        /// </summary>
        /// <param name="ofType">Type of weapon to equip</param>
        /// <exception cref="NullReferenceException"></exception>
        public void EquipWeapon(WeaponType ofType)
        {
            WeaponContainer firstNonEmptyContainer = null;

            firstNonEmptyContainer = holsters.Find(_ => _.type == ofType && _.content != null);

            if (firstNonEmptyContainer == null)
                firstNonEmptyContainer = inventory.Find(_ => _.type == ofType && _.weapon != null);

            if (firstNonEmptyContainer == null)
                throw new NullReferenceException("No weapons in holster or in inventory of a given type.");

            EquipWeapon(firstNonEmptyContainer);
        }

        /// <summary>
        /// Equips weapon by making it the active weapon and
        /// placing it in the trigger hand transform.
        /// If an AimComponent is available, updates its active weapon.
        /// </summary>
        /// <param name="holsterOrWeapon"></param>
        public void EquipWeapon(WeaponContainer holsterOrWeapon)
        {
            if (_active != null && _active.weapon != null)
                // unsubscribe from updates to the magazine of the old weapon
                _active.weapon.onMagazineEmpty -= OnWeaponMagazineEmpty;

            if (holsterOrWeapon is WeaponItem item)
                _active = item;

            else if (holsterOrWeapon is WeaponHolster holster)
                _active = holster.content;

            else _active = null;

            Equip(_active, triggerHandTransform);

            if (_active != null && _active.weapon != null)
                // subscribe from updates to the magazine of the new weapon
                _active.weapon.onMagazineEmpty += OnWeaponMagazineEmpty;

            // attempt to refresh it
            if (aimComponent is null) aimComponent = GetComponent<AimComponent>();

            if (aimComponent != null)
            {
                aimComponent.weapon = _active != null && _active.weapon != null ? _active.weapon : null;
            }

            onWeaponChanged?.Invoke(_active);

        }

        private WeaponType previousWeaponTypeActive = WeaponType.Primary;
        
        /// <summary>
        /// Unequips active weapon to first available holsterOrWeapon of the active type,
        /// or holsterOrWeapon that contains weapon.
        /// </summary>
        public void HolsterActive()
        {
            if (_active != null)
            {
                var active = _active;
                previousWeaponTypeActive = Active;

                foreach (var holster in holsters)
                {
                    if (holster.Contains(active) || holster.content == null)
                    {
                        HolsterWeapon(holster);
                        break;
                    }
                }

                _active = null;
            }

            if (aimComponent != null) aimComponent.weapon = null;
            onWeaponChanged?.Invoke(null);
        }

        /// <summary>
        /// Reparents and transforms weapon to be in
        /// the holder transform.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void Equip(WeaponContainer from, Transform to)
        {
            if (from?.weaponTransform != null)
            {
                from.weaponTransform.SetParent(to);
                ApplyTransformationsToWeapon(from);
            }
        }

        /// <summary>
        /// Reparents weapon in holsterOrWeapon to container
        /// transform and applies transformations.
        /// </summary>
        /// <param name="holster">Container whose transform the weapon will be reparented to.</param>
        public static void HolsterWeapon(WeaponHolster holster)
        {
            if (holster.weaponTransform != null && holster.holsterTransform != null)
            {
                Equip(holster, holster.holsterTransform);
            }
        }

        /// <summary>
        /// Returns currently active weapon if one exists. If none exists
        /// returns an empty item.
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns>True if weapon exists.</returns>
        public bool TryGetActiveWeapon(out WeaponItem weapon)
        {
            var nonNull = _active != null;

            if (nonNull)
            {
                weapon = _active;
                return nonNull;
            }

            weapon = (WeaponItem) null;

            return false;
        }

        private void Start()
        {
            // upon start, holster all weapons
            foreach (var weaponHolster in holsters)
            {
                HolsterWeapon(weaponHolster);
            }

            // if there is an active weapon already assigned, equip it
            if (_active != null) EquipWeapon(_active);

            // set initial animator params
            if (animatorSettings.enabled)
            {
                if (GetAnimator())
                {
                    animatorSettings.ApplyParameters(_animator, _active);
                }
            }

            // update at every weapon change
            onWeaponChanged += item =>
            {
                if (animatorSettings.enabled && GetAnimator())
                {
                    animatorSettings.ApplyParameters(_animator, item);
                }
            };

        }

        bool GetAnimator()
        {
            _animator = GetComponent<Animator>();

            if (!_animator)
            {
                Debug.LogError("Animator settings is enabled but no animator could be found.");
                animatorSettings.Disable();
            }

            return _animator;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
        }

        public static WeaponItem GetCorrectiveTransformsFromAsset(WeaponUser.WeaponItem weaponItem)
        {
            var weaponNotNull = weaponItem.weapon is { };
            var settingsNotNUll = weaponNotNull && weaponItem.weapon.weaponSettings;

            if (!weaponNotNull) Debug.LogWarning("Weapon is null.");
            if (!settingsNotNUll) Debug.LogWarning("Settings is null.");

            if (weaponNotNull && settingsNotNUll)
            {
                var presetSettings = weaponItem.weapon.weaponSettings.inventorySettings;

                weaponItem.correctiveTranslation = presetSettings.correctiveTranslation;
                weaponItem.correctiveRotation = presetSettings.correctiveRotation;
            }

            return weaponItem;
        }

        /// <summary>
        /// Begin reload animation if enough magazines are available.
        /// Otherwise switches weapon to secondary.
        /// </summary>
        private void OnWeaponMagazineEmpty()
        {
            if (
                TryGetActiveWeapon(out var active) && 
                active.weapon != null
            ) {
                if (active.weapon.magazinesInInventory > 0)
                {
                    if (aimComponent is AimComponentWithIK aim) aim.LerpIKFor(active.weapon, 0f, () => { }, 0.1f);
                    _animator.SetTrigger(Reload);
                }
                else if (Active == WeaponType.Primary) // if using primary and no mags in inventory, switch to secondary.
                {
                    _animator.SetTrigger(SwitchWeapon);
                }
            }
        }

        /// <summary>
        /// Animator function. Drops magazine & disables IK.
        /// </summary>
        public void DropMagazine()
        {
            _active?.weapon?.ReloadSystem?.DetachMagazine();
        }

        public void DropWeapon()
        {
            
            if (TryGetActiveWeapon(out var item) && item.weapon != null)
            {
                var weaponController = item.weapon;
                var wpnTransform = weaponController.transform;
                
                wpnTransform.SetParent(null);
                var rb = wpnTransform.gameObject.GetOrElseAddComponent<Rigidbody>();
                rb.useGravity = true;
                
                // remove this if adding weapon pick ups
                Destroy(weaponController.gameObject, 35f);
            }
        }

        /// <summary>
        /// Animator function. Picks up magazine.
        /// </summary>
        public void PickUpNewMagazine()
        {
            if (TryGetActiveWeapon(out var weapon) && weapon.weapon != null)
            {
                var mag = weapon.weapon.ReloadSystem.magazine;
                if (mag != null)
                {
                    mag.transform.SetParent(reloadHandTransform);
                    // from editor testing
                    mag.transform.localPosition = new Vector3(-0.064000003f,0.0270000007f,0.0209999997f);
                    mag.transform.localRotation = new Quaternion(-0.354237527f, 0.451902986f, 0.607355833f, -0.54901588f);
                    mag.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Animator function. Attaches mag & reenables IK.
        /// </summary>
        public void AttachMagazineToWeapon()
        {
            if (TryGetActiveWeapon(out var weapon) && weapon.weapon != null)
            {
                weapon.weapon.ReloadSystem.AttachMagazine();
                
                if (aimComponent is AimComponentWithIK aim)
                    aim.LerpIKFor(weapon.weapon, 1f, () => { });
            }
        }

        private void OnDrawGizmos()
        {
        }
    }
}