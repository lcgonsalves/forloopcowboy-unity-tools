using System;
using System.Collections;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.Weapon
{
    [RequireComponent(typeof(BulletSystem), typeof(ReloadSystem)), SelectionBase]
    public class WeaponController : MonoBehaviour
    {
        public Weapon weaponSettings;

        // Exposed state

        [SerializeField]
        public int bulletsInClip = 0;

        public int magazinesInInventory = 2;

        [SerializeField, ReadOnly]
        private bool currentlyFiring = false;

        public bool isFiring { get => currentlyFiring; private set { currentlyFiring = value; } }

        public bool shouldFire = false;

        /// <summary> When one is defined, the direction of the bullet will be calculated from the muzzle to the target position. </summary>
        [CanBeNull] private AccuracyProcessor.ScrambledTransform _target;
    
        [Serializable]
        public struct BurstSettings
        {
            public bool enabled;
        
            public float maxBurstLength;
            public float minBurstLength;
        
            public float maxBreakLength;
            public float minBreakLength;
        }

        public BurstSettings burstSettings;

        // internal variables

        public Transform muzzle { get; private set; }

        private Coroutine firingCoroutine;
        private Coroutine burstCoroutine;

        public List<ParticleSystem> peripheralEmitters = new List<ParticleSystem>();

        public event Action onMagazineEmpty;

        private BulletSystem _bulletSystem;

        void Start()
        {
            // begin loaded
            bulletsInClip = weaponSettings.clipSize;
            
            // initialize settings if available
            burstSettings = weaponSettings ? weaponSettings.defaultBurstSettings : burstSettings;

            // cache gameobject
            muzzle = Weapon.MuzzlePosition(gameObject);

            // begin firing coroutine
            firingCoroutine = StartCoroutine(FiringCoroutine());
        
            // begin burst managment coroutine
            burstCoroutine = StartCoroutine(BurstCoroutine());
            
            // cache bullet system
            _bulletSystem = GetComponent<BulletSystem>();

        }

        /// <summary>
        /// This coroutine spawns and fires bullets at a regular interval defined by
        /// the rate of fire of the weapon.
        /// </summary>
        private IEnumerator FiringCoroutine()
        {
            // TODO: move to weapon settings.
            var range = 10f;
            
            // do while alive
            while (true)
            {
                while (shouldFire && isFiring && bulletsInClip > 0)
                {
                    // if a scramble target is defined, we use the direction from the muzzle to the target
                    // otherwise just fire straight.
                    var muzzleDirection = muzzle.forward;
                    var muzzlePosition = muzzle.position;
                    
                    if (_target != null)
                    {
                        var distance = Vector3.Distance(muzzlePosition, _target.transform.position);
                        muzzleDirection = _target.GetScrambledPositionAtRange(distance / range) - muzzlePosition;
                    }

                    if (_target != null)
                    {
                        Debug.DrawRay(muzzlePosition, muzzleDirection);
                    }
                    
                    _bulletSystem.SpawnAndFire(weaponSettings.ammo, muzzlePosition, muzzleDirection);

                    if (weaponSettings.muzzleEffect != null)
                    {
                        var muzzleFlash = Instantiate(weaponSettings.muzzleEffect, muzzle);
                        muzzleFlash.transform.rotation = muzzle.rotation;
                        Destroy(muzzleFlash, 1f);
                    }

                    foreach (var emitter in peripheralEmitters)
                    {
                        emitter.Emit(1);
                    }

                    bulletsInClip--;

                    if (bulletsInClip == 0) onMagazineEmpty?.Invoke();

                    yield return new WaitForSeconds(60f / weaponSettings.bulletsPerMinute);
                }

                isFiring = false;

                // if not firing check every frame
                yield return new WaitForEndOfFrame();

            }
        }
    
    
        /// <summary>
        /// This coroutine enables and disables firing at random but bounded intervals, simulating a burst.
        /// </summary>
        private IEnumerator BurstCoroutine()
        {
            while (true)
            {
                if (burstSettings.enabled && shouldFire)
                {
                    var burstTime = Random.Range(burstSettings.minBurstLength, burstSettings.maxBurstLength);
                    var breakTime = Random.Range(burstSettings.minBreakLength, burstSettings.maxBreakLength);

                    isFiring = !isFiring; // toggle

                    // pick correct wait time depending on whether it was firing or waiting
                    yield return isFiring ? new WaitForSeconds(burstTime) : new WaitForSeconds(breakTime);
                }
                else yield return null;
            }
        }

        /// <summary>
        /// Begins firing bullets.
        /// </summary>
        /// <param name="burst">When true, uses burst logic. When false, fires on full-auto.</param>
        /// <param name="scrambledTarget">When set, the gun will shoot towards the target's scrambled position.</param>
        public void OpenFire(bool burst = false, [CanBeNull] AccuracyProcessor.ScrambledTransform scrambledTarget = null)
        {
            burstSettings.enabled = burst;
            shouldFire = true;
            if (!burst) isFiring = true;
            _target = scrambledTarget;
        }

        public void CeaseFire()
        {
            shouldFire = false;
            _target = null;
        }
        
        /// <returns>True if there were enough magazines to reload.</returns>
        public bool Reload()
        {
            bool hasMag = magazinesInInventory > 0;
            if (hasMag)
            {
                bulletsInClip = weaponSettings.clipSize;
                magazinesInInventory--;
            }

            return hasMag;
        }

        private ReloadSystem _rs; 
        public ReloadSystem ReloadSystem => _rs ? _rs : _rs = GetComponent<ReloadSystem>();

        private void OnDrawGizmosSelected()
        {
            if (_target != null)
            {
                Gizmos.color = new Color(1f, 0.58f, 0f);
                Gizmos.DrawSphere(_target.lastScrambledPosition, 0.12f);
            }
        }
    }
}
