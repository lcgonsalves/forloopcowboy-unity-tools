using ForLoopCowboyCommons.EditorHelpers;
using ForLoopCowboyCommons.Damage;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Linq;

// Exposes public methods to control the behavior of a soldier
public class SoldierBehaviour : MonoBehaviour
{
    // Inputs

    [SerializeField]
    public Soldier identity;

    [SerializeField]
    private Army _armyAssociation;
    public Army armyAssociation
    {
        get => _armyAssociation;
        set
        {
            GameObjectHelpers.SetLayerRecursively(gameObject, value.Layer);
            _armyAssociation = value;
        }
    }

    public List<Army> enemies = new List<Army>();
    public Weapon weapon;

    [Header("Size of array allocated for scanning for enemies.")]
    public int SphereAllocSize = 30;

    [Tooltip("Time between coroutine passes to check if there's an enemy in the vicinity.")]
    public float enemySpotInterval = 0.5f;

    [Tooltip("Layer containing world geometry to detect lines of sight.")]
    public LayerMask environmentLayerMask;

    public enum BehaviourStates
    {
        Idle,
        Aware,
        Moving,
        Aiming,
        TakingCover,
        Reloading,
        TakingDamage,
        Dying,
        Ragdoll,
        Engage
    }

    // consts
    protected int VELOCITY;

    [ReadOnly]
    public List<Rigidbody> rigidbodies = new List<Rigidbody>(11);

    [ReadOnly, Tooltip("Which transform will parent the gun.")]
    public Transform rightHandReference;

    [ReadOnly, Tooltip("Which transform will be used to cast line-of-sight rays from.")]
    public Transform headReference;

    [ReadOnly, Tooltip("Which transform will be used as a target")]
    public Transform chestReference;

    public Vector3 eyesightSourcePosition { get => headReference?.position ?? transform.position; }

    [ReadOnly]
    public bool isRagdolling = false;


    [SerializeField, ReadOnly]
    private SoldierBehaviour _target;

    // cached references

    public NavMeshAgent navigation { get; private set; }

    public Animator animator { get; private set; }

    private GameObject weaponRef;

    private LayerMask enemyLayerMask = 0;

    public WeaponController weaponController { get; private set; }

    public SoldierAimComponent aim { get; private set; }

    public SoldierBehaviourStateManager stateManager { get; private set; }

    // Exposed state

    private readonly HashSet<SoldierBehaviour> _spottedHashSet = new HashSet<SoldierBehaviour>();

    public HashSet<SoldierBehaviour> spotted { get => _spottedHashSet; }

    [SerializeField, ReadOnly]
    private int health = 100;

    public int Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0, 100);
            if (health == 0) onDeath?.Invoke();
        }
    }

    private bool isOverridingVelocity = false;
    public float currentVelocity { get => isOverridingVelocity ? overrideVelocity : navigation.velocity.magnitude; }
    private float overrideVelocity = 0f; // used to force animation

    // Coroutine refs

    private Coroutine scan, readState;

    // event emitters

    public event Action onDeath;


    // Start is called before the first frame update
    void Start()
    {

        // find all rigid bodies in the armature
        GetComponentsInChildren<Rigidbody>(rigidbodies);

        // cache or create navmeshagent
        navigation = GetComponent<NavMeshAgent>();
        if (!navigation) navigation = gameObject.AddComponent<NavMeshAgent>();

        // cache or log error on animator
        animator = GetComponent<Animator>();
        if (!animator) Debug.LogError("Must have an animator with 'velocity' parameter defined");

        stateManager = new SoldierBehaviourStateManager(this);

        VELOCITY = Animator.StringToHash("velocity");

        rightHandReference = transform.Find("Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand");
        headReference = transform.Find("Armature/Hips/Spine/Spine1/Spine2/Neck/Head/Head_end");
        chestReference = transform.Find("Armature/Hips/Spine/Spine1");

        // init weapon
        InitializeWeapon();

        // cache or create aim component
        aim = GetComponent<SoldierAimComponent>();
        if (!aim)
        {

            // only initialize settings if no component exists
            aim = gameObject.AddComponent<SoldierAimComponent>();
            aim.soldierSettings = identity;
            aim.eyeLevel = headReference;
            aim.aimAnimationLayerName = "Aiming";

        }

        // pass weapon reference to aim component
        aim.weaponTransform = weaponRef.transform;

        // begin with ragdoll disabled
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
            var bulletDetector = rb.gameObject.AddComponent<SoldierLimbComponent>();
            bulletDetector.parent = this;
        }

        // set layer recursively
        if (_armyAssociation) armyAssociation = _armyAssociation;

        // initialize layermask to contain all enemy layers
        foreach (var enemy in enemies) { enemyLayerMask |= enemy.LayerMask; }

        // remove self army layer from mask
        enemyLayerMask &= ~armyAssociation.LayerMask;


        // begin spotting coroutine
        scan = StartCoroutine(ScanAndSpotSoldiers());
        readState = StartCoroutine(StateProcessingCoroutine());

    }

    // Instantiates weapon in right hand and stores local reference

    [SerializeField, ReadOnly]
    private Transform weaponGrabHandle;
    private void InitializeWeapon()
    {

        if (weapon && !weaponRef)
        {

            weaponRef = Instantiate(
                weapon.prefab,
                rightHandReference.TransformPoint(weapon.weaponPosition),
                Quaternion.identity,
                rightHandReference
            );

            weaponRef.transform.localPosition = weapon.weaponPosition;
            weaponRef.transform.localRotation = weapon.weaponRotation;

            weaponGrabHandle = Weapon.GrabPointB(weaponRef);

            weaponController = weaponRef.GetComponent<WeaponController>();

            if (!weaponController) Debug.LogError("Instantiated weapon does not have a WeaponController attached!");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {

        if (!animator) return;

        // disable IK when dying
        if (stateManager != null && stateManager.currentState == SoldierBehaviourStateManager.State.Dying)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
        }
        // make left hand hold grab point of the weapon
        else if (weaponGrabHandle)
        {

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, weaponGrabHandle.position);


        }
    }

    private void OnGUI()
    {

        float avgV = 0f;
        foreach (var rb in rigidbodies) { avgV += rb.velocity.magnitude; }
        avgV = avgV / rigidbodies.Count;

        GUI.Label(new Rect(10, 10, 600, 100), $"Average velocity {avgV}");
    }

    public void DropWeapon()
    {
        var weaponRB = weaponRef.gameObject.AddComponent<Rigidbody>();
        weaponRef.transform.parent = null;
    }

    public void EnableRagdoll()
    {
        // kill animator
        animator.enabled = false;

        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
            isRagdolling = true;
        }
    }

    public void DisableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
            isRagdolling = false;
        }
    }

    public SoldierBehaviour targetSoldier
    {
        get => _target;
        set => _target = value;
    }

    // If a target is set, iterates over its colliders and raycasts from eye position through environment.
    // Returns the transform of the collider if one is in view unobstructed or null otherwise.
    public Transform firstAvailableTargetColliderInview
    {
        get
        {
            Transform[] potentialTargets = {
                targetSoldier?.headReference,
                targetSoldier?.chestReference
            };

            Transform availableTarget = null;

            for (int i = 0; i < potentialTargets.Length; i++)
            {
                RaycastHit hit;
                Transform potentialTarget = potentialTargets[i];

                // return true if raycast hits AND that it hits a SoldierBehaviour
                if (
                    (potentialTarget != null) && 
                    Physics.Raycast(
                        eyesightSourcePosition, 
                        (potentialTarget.position - eyesightSourcePosition).normalized,
                        out hit,
                        100f,
                        enemyLayerMask | environmentLayerMask
                    ) && 
                    (hit.transform.GetComponent<SoldierLimbComponent>() != null)
                ) availableTarget = potentialTarget;

            }

            if (availableTarget && weaponController.muzzle) Debug.DrawLine(weaponController.muzzle.position, availableTarget.position, Color.blue);

            return availableTarget;
        }
    }

    // updates animator and triggers nav mesh agent to walk to position
    // if navigator is disabled, enable it
    public void WalkTo(Vector3 position)
    {
        if (stateManager.currentState == SoldierBehaviourStateManager.State.Dying) return;

        // stop crouching
        stateManager.SetCrouch(false);
        aim.WeaponDown(() => { if (!navigation.enabled) { navigation.enabled = true; } navigation.SetDestination(position); navigation.isStopped = false; });
    }

    public void StopWalkingAndScan()
    {
        if (navigation.enabled) aim.WeaponDown(() => {
                navigation.enabled = false;
        });

    }

    // moves transform to position instantly
    public void TeleportTo(Vector3 position)
    {
        navigation.enabled = false;
        transform.position = position;
        navigation.enabled = true;
    }

    private Coroutine snappingCoroutineInstance;

    // snaps using default transition, DISABLES NAVIGATION
    public void SnapTo(Vector3 position) { SnapTo(position, Transition.Linear); }
    public void SnapTo(Vector3 position, Transition transition)
    {
        // always override previous snap coroutine
        if (snappingCoroutineInstance != null) StopCoroutine(snappingCoroutineInstance);

        // start snapping coroutine
        snappingCoroutineInstance = StartCoroutine(SnapToCoroutine(position, transition));

    }

    private IEnumerator SnapToCoroutine(Vector3 position, Transition transition)
    {
        var animation = transition.GetPlayableInstance();
        var startPosition = transform.position;

        // disable navigation while snapping
        navigation.enabled = false;

        // enable velocity override
        isOverridingVelocity = true;

        while (!animation.finished)
        {
            // interpolate between current position and final position using animation
            var destination = Vector3.Lerp(startPosition, position, animation.Evaluate(Time.deltaTime));
            // look at vector but use same Y value not to pitch-rotate transform
            Vector3 lookAt = (new Vector3(destination.x, transform.position.y, destination.z) - transform.position).normalized;
            lookAt = Mathf.Approximately(lookAt.magnitude, 0f) ? transform.TransformDirection(Vector3.forward) : lookAt;

            // distance/time is the velocity this frame. override it. since we do it every frame the delta time is used
            // reduce it a bit
            overrideVelocity = Vector3.Distance(transform.position, destination) * 0.5f / Time.deltaTime;

            debugPosition = destination;

            transform.rotation = Quaternion.LookRotation(lookAt, Vector3.up);
            transform.position = destination;
            yield return null;
        }

        // stop overriding velocity when done
        isOverridingVelocity = false;

        // crouch
        stateManager.SetCrouch(true);

    }

    // Coroutine for scanning and spotting living soldiers
    Collider[] detected;
    private IEnumerator ScanAndSpotSoldiers()
    {
        /* Collider[] */
        detected = new Collider[SphereAllocSize];

        while (true)
        {

            // sphere check around soldier, filter for all targets not of the same army association
            var numColliders = Physics.OverlapSphereNonAlloc(transform.position, identity.visibilityRange, detected, enemyLayerMask);
            for (int i = 0; i < numColliders; i++)
            {
                var enemySoldierCollider = detected[i];

                var component = enemySoldierCollider.GetComponentInParent<SoldierBehaviour>();
                if (!component) continue; // skip objects that are not soldiers

                // default to transform if no head attached for some reason
                Ray r = new Ray(eyesightSourcePosition, (enemySoldierCollider.transform.position - eyesightSourcePosition).normalized);
                RaycastHit hit;

                // raycast on environment layer to see if there's obstructions
                if (Physics.Raycast(r, out hit, 100f, environmentLayerMask | enemyLayerMask))
                {
                    if (/* hit.transform.GetComponentInParent<SoldierBehaviour>() && */ component.Health > 0) { spotted.Add(component); }
                }

            }

            // reset target if no spotted
            if (spotted.Count == 0) targetSoldier = null;

            yield return new WaitForSeconds(enemySpotInterval);

            // O(n)
            Array.Clear(detected, 0, detected.Length);
            spotted.Clear();
        }
    }

    // read state runs parallel to Update() processes state each frame.
    private IEnumerator StateProcessingCoroutine()
    {
        while (true) { stateManager.ProcessState(); yield return null; }
    }

    // DEBUG
    private void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, identity.visibilityRange);

        Gizmos.color = Color.green;
        // draw all colliders detected in spotting coroutine
        if (detected != null) foreach (var collider in detected)
            {
                if (collider != null) Gizmos.DrawWireSphere(collider.transform.position, 0.2f);
            }

        Gizmos.color = Color.red;
        foreach (SoldierBehaviour s in spotted)
        {
            if (!s) continue;
            Gizmos.DrawLine(headReference.transform.position, s.transform.position);
            Gizmos.DrawSphere(new Vector3(s.transform.position.x, s.transform.position.y + 2f, s.transform.position.z), 0.15f);
        }

    }

    Vector3 debugPosition = Vector3.zero;
    private void OnDrawGizmos()
    {
        Handles.color = Color.blue;

        Handles.Label(transform.TransformPoint(new Vector3(0, 2f, 0)), "Health " + Health);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(debugPosition, 0.15f);

        if (weaponController) Handles.Label(transform.TransformPoint(new Vector3(0, 2.2f, 0)), "Ammo " + weaponController.BulletsInClip.ToString());
        if (stateManager != null) Handles.Label(transform.TransformPoint(new Vector3(0, 2.4f, 0)), "State " + stateManager.currentState.ToString());
    }

    private void OnDestroy()
    {
        DestroyImmediate(weaponRef);
        spotted.Clear();
        weaponRef = null;
    }

    public void Damage(int amount) { health = Mathf.Clamp(health - amount, 0, 100); }

    public void Heal(int amount) { health = Mathf.Clamp(health + amount, 0, 100); }


    /// Component to independently handle collision on each limb.

    public class SoldierLimbComponent : MonoBehaviour
    {
        internal SoldierBehaviour parent;

        // if dying and detect collision, disable animator and imediately handle everything with physics

        private void OnParticleCollision(GameObject other)
        {

            SimpleDamageProvider damageProvider;

            if (other.gameObject.CompareTag(DamageSystem.tag) && other.gameObject.TryGetComponent<SimpleDamageProvider>(out damageProvider))
            {
                parent.Damage(damageProvider.GetDamageAmount());
            }

        }

        private void OnCollisionEnter(Collision other) {
            if (other.gameObject.CompareTag("Missile"))
            {
                parent.EnableRagdoll();
            }
        }

    }

}
