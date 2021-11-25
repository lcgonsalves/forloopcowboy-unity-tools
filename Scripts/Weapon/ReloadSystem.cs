using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Weapon
{
    public class ReloadSystem : MonoBehaviour
    {
        public GameObject magazine;

        private Vector3 _initialLocalPosition;
        private Transform _initialParent;

        private void Start()
        {
            _initialLocalPosition = magazine.transform.localPosition;
            _initialParent = magazine.transform.parent;
        }
        
        /// <returns>Dropped magazine, or null if no magazine is used.</returns>
        public GameObject DetachMagazine(float? autoDestroyDelay = null)
        {
            if (!magazine) return null;
            
            var droppedMagazine = magazine;

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
            
            magazine.SetActive(true);
        }
    }
}