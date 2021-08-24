using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using ForLoopCowboyCommons.EditorHelpers;
using ForLoopCowboyCommons.Damage;
using Random = UnityEngine.Random;

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

    private ParticleSystem bulletEmitter;

    public List<ParticleSystem> peripheralEmitters = new List<ParticleSystem>();

    void Start()
    {
        // begin loaded
        bulletsInClip = weaponSettings.clipSize;

        // cache gameobject
        muzzle = Weapon.MuzzlePosition(gameObject);

        // cache emitter
        bulletEmitter = GetComponentInChildren<ParticleSystem>();
        if (!bulletEmitter) {
           bulletEmitter = gameObject.AddComponent<ParticleSystem>();
           Debug.LogError("Gun must have a particle system in the hierarchy.");
        }

        // attach damage component to emitter
        var dmgProvider = bulletEmitter.gameObject.AddComponent<SimpleDamageProvider>();
        dmgProvider.min = weaponSettings.minimumDamage;
        dmgProvider.max = weaponSettings.maximumDamage;

        // begin firing coroutine
        firingCoroutine = StartCoroutine(FiringCoroutine());
        
        // begin burst managment coroutine
        burstCoroutine = StartCoroutine(BurstCoroutine());

    }

    private IEnumerator FiringCoroutine()
    {
        // do while alive
        while (true)
        {
            while (shouldFire && isFiring && bulletsInClip > 0)
            {
                bulletEmitter.Emit(1);
                foreach (var emitter in peripheralEmitters)
                {
                    // todo: use bullet system here instead
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
