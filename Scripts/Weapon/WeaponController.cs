using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ForLoopCowboyCommons.EditorHelpers;
using ForLoopCowboyCommons.Damage;

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

    // internal variables

    public Transform muzzle { get; private set; }

    private Coroutine firingCoroutine;

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

        // begin coroutine
        firingCoroutine = StartCoroutine(FiringCoroutine());

    }

    private IEnumerator FiringCoroutine()
    {
        // do while alive
        while (true)
        {

            while (isFiring && bulletsInClip > 0)
            {
                bulletEmitter.Emit(1);
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

    public void OpenFire()
    {
        isFiring = true;
    }

    public void CeaseFire()
    {
        isFiring = false;
    }

    public void Reload() { bulletsInClip = weaponSettings.clipSize; }

}
