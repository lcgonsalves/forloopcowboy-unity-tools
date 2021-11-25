using System;
using System.Collections;
using System.Collections.Generic;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.Weapon
{
    [RequireComponent(typeof(BulletSystem), typeof(ReloadSystem)), SelectionBase]
    public class WeaponController : MonoBehaviour
    {
        public Weapon weaponSettings;

        // Exposed state

        [SerializeField, ReadOnly]
        public int bulletsInClip = 0;

        [SerializeField, ReadOnly]
        private bool currentlyFiring = false;

        public bool isFiring { get => currentlyFiring; private set { currentlyFiring = value; } }

        public bool shouldFire = false;
    
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

        private BulletSystem _bulletSystem;

        void Start()
        {
            // begin loaded
            bulletsInClip = weaponSettings.clipSize;

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
            // do while alive
            while (true)
            {
                while (shouldFire && isFiring && bulletsInClip > 0)
                {
                    _bulletSystem.SpawnAndFire(weaponSettings.ammo, muzzle.position, muzzle.forward);

                    foreach (var emitter in peripheralEmitters)
                    {
                        emitter.Emit(1);
                    }

                    bulletsInClip--;

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

        public void OpenFire(bool burst = false)
        {
            burstSettings.enabled = burst;
            shouldFire = true;
        }

        public void CeaseFire()
        {
            shouldFire = false;
        }

        public void Reload() { bulletsInClip = weaponSettings.clipSize; }

        private ReloadSystem _rs; 
        public ReloadSystem ReloadSystem => _rs ? _rs : _rs = GetComponent<ReloadSystem>();

    }
}
