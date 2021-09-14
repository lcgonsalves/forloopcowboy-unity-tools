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
    [RequireComponent(typeof(BulletSystem)), SelectionBase]
    public class WeaponController : MonoBehaviour
    {
        public Weapon weaponSettings;

        // Exposed state

        [SerializeField, ReadOnly]
        private int bulletsInClip = 0;

        public int BulletsInClip { get => bulletsInClip; }

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

    }
}
