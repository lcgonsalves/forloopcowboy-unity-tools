using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using ForLoopCowboyCommons.EditorHelpers;

/// Exposes methods for aiming at points
public class SoldierAimComponent : MonoBehaviour
{
    public Soldier soldierSettings;

    [Tooltip("Where the height is calculated from.")]
    public Transform eyeLevel;

    [Tooltip("This means that the character can look up maximum 60 deg")]
    public Vector2 maxLookAngle = new Vector2(60, 50);
    public Vector2 minLookAngle = new Vector2(-60, -50);

    public string aimAnimationLayerName;

    // cached refs
    private Animator animator;

    public Transform weaponTransform;

    private Transition.TransitionState easeToAimTransition;
    private Transition.TransitionState aimToEaseTransition;

    private int AimAnimationLayer;

    [SerializeField, ReadOnly]
    private bool _isTracking = false;
    public bool isTracking { get => _isTracking; private set => _isTracking = value; }

    private Transform trackedTarget = null;

    private void Start()
    {

        animator = GetComponent<Animator>();
        if (!animator) { Debug.LogError("this component requires an animator at enable time."); this.enabled = false; return; }

        easeToAimTransition = soldierSettings.easeToAimTransition.GetPlayableInstance();
        aimToEaseTransition = soldierSettings.aimToEaseTransition.GetPlayableInstance();

        AimAnimationLayer = animator.GetLayerIndex(aimAnimationLayerName);

    }

    private void Update()
    {
        if (isTracking && (trackedTarget != null)) { Aim(trackedTarget.position); }
        else { WeaponDown(); }
    }

    // Makes component execute Aim() on Update() loop, focusing on target's position.
    public void Track(Transform target)
    {
        isTracking = true;
        trackedTarget = target;
    }

    public void StopTracking()
    {
        isTracking = false;
        trackedTarget = null;
    }

    // Aims towards target and executes callback when aim is ready. Can be called in update loop.
    public void Aim(Vector3 target, Action onAimReady)
    {

        // if was stopping aim, cancel it
        aimToEaseTransition.Stop();

        float distanceToTarget = Vector3.Distance(eyeLevel.position, target);
        float relativeTargetHeight = target.y - eyeLevel.position.y;
        float verticalLookAngle = Mathf.Asin(relativeTargetHeight / distanceToTarget) * Mathf.Rad2Deg;

        // map this into the angle clamp to send to animator
        float relativeAimHeight = verticalLookAngle > 0 ? (verticalLookAngle / maxLookAngle.x) : (verticalLookAngle / minLookAngle.y * -1);

        animator?.SetFloat(SoldierBehaviourStateManager.StateParameters.YAimAngle.ToString(), relativeAimHeight);

        // rotate root transform to face
        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));

        Action callback = () =>
        {
            // rotate gun to align when ready
            if (weaponTransform != null)
            {
                weaponTransform.LookAt(target);
            }

            onAimReady();
        };

        // Check when animation state is ready and then callback
        if (animator.GetLayerWeight(AimAnimationLayer) < 1 && !easeToAimTransition.isPlaying)
        {
            StartCoroutine(TransitionToAimDownSights(callback));
        }
        else callback();

    }
    public void Aim(Vector3 target) { Aim(target, () => { }); }


    public void WeaponDown(Action onWeaponDown)
    {
        // if was aiming, cancel
        easeToAimTransition.Stop();

        if (animator && animator.GetLayerWeight(AimAnimationLayer) > 0.1f && !aimToEaseTransition.isPlaying)
        {
            StartCoroutine(TransitionToWeaponDown(onWeaponDown));
        } else onWeaponDown();
    }

    public void WeaponDown() { WeaponDown(() => { }); }

    private IEnumerator TransitionToAimDownSights(Action callback)
    {

        easeToAimTransition.ResetAnimation();

        while (!easeToAimTransition.finished)
        {
            float weight = easeToAimTransition.Evaluate(Time.deltaTime);
            animator?.SetLayerWeight(AimAnimationLayer, weight);

            yield return null;
        }

        // after animation finishes, clean up
        callback();

    }

    private IEnumerator TransitionToWeaponDown(Action callback)
    {

        aimToEaseTransition.ResetAnimation();

        while (!aimToEaseTransition.finished)
        {
            float weight = aimToEaseTransition.Evaluate(Time.deltaTime);
            animator?.SetLayerWeight(AimAnimationLayer, weight);

            yield return null;
        }

    }

    private void OnDrawGizmosSelected()
    {
        Handles.Label(new Vector3(transform.position.x, transform.position.y + 4f, transform.position.z), $"ADS anim: IsPlaying {easeToAimTransition?.isPlaying} isFinished {easeToAimTransition?.finished} ");
        Handles.Label(new Vector3(transform.position.x, transform.position.y + 4.2f, transform.position.z), $"Stop Aim anim: IsPlaying {aimToEaseTransition?.isPlaying} isFinished {aimToEaseTransition?.finished} ");
    }

}
