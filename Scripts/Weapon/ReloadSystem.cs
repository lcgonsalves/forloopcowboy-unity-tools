using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Weapon
{
    public class ReloadSystem : MonoBehaviour
    {
        public GameObject magazine;

        [SerializeField, ReadOnly]
        private Vector3 _initialLocalPosition;
        
        [SerializeField, ReadOnly]
        private Quaternion _initialLocalRotation;
        
        [SerializeField, ReadOnly]
        private Transform _initialParent;

        private WeaponController _weaponController;

        private void Start()
        {
            if (magazine)
            {
                _initialLocalPosition = magazine.transform.localPosition;
                _initialLocalRotation = magazine.transform.localRotation;
                _initialParent = magazine.transform.parent;
            }

            _weaponController = GetComponent<WeaponController>();
        }
        
        /// <returns>Dropped magazine, or null if no magazine is used.</returns>
        public GameObject DetachMagazine(float? autoDestroyDelay = null)
        {
            if (!magazine) return null;
            
            var droppedMagazine = magazine;

            if (_weaponController) _weaponController.bulletsInClip = 0;

                magazine = Instantiate(magazine, _initialParent, true);
            magazine.SetActive(false);
            
            droppedMagazine.transform.SetParent(null, true);
            
            var rb = droppedMagazine.gameObject.GetOrElseAddComponent<Rigidbody>();
            rb.useGravity = true;
            
            if (autoDestroyDelay.HasValue)
                Destroy(droppedMagazine, autoDestroyDelay.Value);

            return droppedMagazine;
        }

        /// <summary>
        /// Reparents and activates magazine object back to initial position, if one exists.
        /// </summary>
        public void AttachMagazine()
        {
            if (!magazine) return;

            magazine.transform.SetParent(_initialParent);
            magazine.transform.localPosition = _initialLocalPosition;
            magazine.transform.localRotation = _initialLocalRotation;
            
            magazine.SetActive(true);

            if (_weaponController) _weaponController.bulletsInClip = _weaponController.weaponSettings.clipSize;
        }
    }
}