using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ForLoopCowboyCommons.EditorHelpers;	

[RequireComponent(typeof(Animator))]
public class ArmComponent : MonoBehaviour
{
    [Header("Animator Parameters"), ReadOnly]
    public string TwoFingerHoldBool = "TwoFingerHold";

    [ReadOnly]
    public string OpenPalmHoldBool = "OpenPalmHold";
    
    [ReadOnly]
    public string ClosedPalmHoldBool = "ClosedPalmHold";

    [ReadOnly]
    public string CastThrowTrigger = "CastThrow";

    [ReadOnly]
    public string CastForwardBool = "CastForward";

    [Header("Configuration")]

    public bool debugMode = false;

    Animator animator;

    // Triggers every when arm is running cast animation and it reaches the point where the projectile should be launched
    public event Action OnCast;

    // Triggers when hold animation reaches critical holding point (i.e. arbitrary)

    public event Action OnHoldReady;

    // Defines the possible ways to hold a spell
    public enum ChargeStyles
    {
        TwoFingerHold,
        OpenPalmHold,
        ClosedPalmHold
    }

    // Defines the possible ways to cast a spell
    public enum CastStyles
    {
        CastForward,
        CastThrow
    }

    public IEnumerable<ChargeStyles> chargeStyles = EnumUtil.GetValues<ChargeStyles>();

    public IEnumerable<CastStyles> castStyles = EnumUtil.GetValues<CastStyles>();

    public enum ArmPosition
    {
        Thumb,
        IndexFinger,
        MiddleFinger,
        RingFinger,
        PinkyFinger,
        Palm
    }

    // Set to true when current hold animation reaches critical point, sets to false when cast event fires or idle animation starts
    public bool holdReady = false;

    private Dictionary<ArmPosition, Transform> armTransforms = new Dictionary<ArmPosition, Transform> {
        { ArmPosition.Thumb, null },
        { ArmPosition.IndexFinger, null },
        { ArmPosition.MiddleFinger, null },
        { ArmPosition.RingFinger, null },
        { ArmPosition.PinkyFinger, null },
        { ArmPosition.Palm, null }
    };

    private void OnEnable() {
        InitializeFingers();
    }

    void Start()
    {
       animator = GetComponent<Animator>();
       if (!animator) Debug.LogError(name + " has no Animator component!");  
    }

    void InitializeFingers()
    {
        string basePath = "Armature/Bone/Bone.002/";
        Func<int, string> bone = i => (i < 10 ? "Bone.00" + i : "Bone.0" + i) + "/"; // no bones over 100

        armTransforms[ArmPosition.Thumb] = 
            transform.Find(basePath + bone(3) + bone(8) + bone(9) + ArmPosition.Thumb.ToString());

        Debug.Log(basePath + bone(3) + bone(8) + bone(9) + ArmPosition.Thumb.ToString());

        armTransforms[ArmPosition.IndexFinger] =
            transform.Find(basePath + bone(4) + bone(10) + bone(11) + bone(12) + ArmPosition.IndexFinger.ToString());

        armTransforms[ArmPosition.MiddleFinger] =
            transform.Find(basePath + bone(5) + bone(13) + bone(14) + bone(15) + ArmPosition.MiddleFinger.ToString());

        armTransforms[ArmPosition.RingFinger] =
            transform.Find(basePath + bone(6) + bone(16) + bone(17) + bone(18) + ArmPosition.RingFinger.ToString());

        armTransforms[ArmPosition.PinkyFinger] =
            transform.Find(basePath + bone(7) + bone(19) + bone(20) + bone(21) + ArmPosition.PinkyFinger.ToString());

        armTransforms[ArmPosition.Palm] =
            transform.Find(basePath + ArmPosition.Palm.ToString());
    }

    // Returns the transform of the given finger, or the root transform if the finger is not found.
    public Transform GetArmPosition(ArmPosition finger)
    {
        if (armTransforms.TryGetValue(finger, out var fingerTransform))
        {
            return fingerTransform;

        } else {
            Debug.LogError("Finger " + finger + " not initialized. Returning root transform. Please configure bone hierarchy properly");
            return transform;
        }
    }

    public Vector3 GetCastPoint(ChargeStyles holdState)
    {
        switch (holdState)
        {
            // Two-finger hold: Cast point is between the index and middle finger positions
            case ChargeStyles.TwoFingerHold:
                return Vector3.Lerp(
                    GetArmPosition(ArmPosition.IndexFinger).position,
                    GetArmPosition(ArmPosition.MiddleFinger).position,
                    0.5f
                );
            case ChargeStyles.OpenPalmHold:
            case ChargeStyles.ClosedPalmHold:
                return GetArmPosition(ArmPosition.Palm).position;

            default:
                Debug.LogError("Invalid hold state: " + holdState);
                return transform.position;

        }
    }

    // Toggles the hold state
    public void ToggleHolder(ChargeStyles state)
    {
        var isHolding = IsHolding(state);

        switch (state)
        {
            case ChargeStyles.TwoFingerHold:
                animator.SetBool(TwoFingerHoldBool, !isHolding);
                break;

            case ChargeStyles.OpenPalmHold:
                animator.SetBool(OpenPalmHoldBool, !isHolding);
                break;

            case ChargeStyles.ClosedPalmHold:
                animator.SetBool(ClosedPalmHoldBool, !isHolding);
                break;
        }
    }

    public bool IsHoldingAndReady(ChargeStyles state)
    {
        return IsHolding(state) && holdReady;
    }

    // True when animator parameter for given charge style is true && hold animation has reached critical point (ready)
    public bool IsHolding(ChargeStyles state)
    {
        return AnimatorStateParameterIsHolding(state);

        bool AnimatorStateParameterIsHolding(ChargeStyles state)
        {
            switch (state)
            {
                case ChargeStyles.TwoFingerHold:
                    return animator.GetBool(TwoFingerHoldBool);

                case ChargeStyles.OpenPalmHold:
                    return animator.GetBool(OpenPalmHoldBool);

                case ChargeStyles.ClosedPalmHold:
                    return animator.GetBool(ClosedPalmHoldBool);

                default:
                    return false;
            }
        }
    }

    public bool IsHolding()
    {
        bool isHolding = false;

        foreach (var style in chargeStyles)
        {
            isHolding |= IsHolding(style);
        }

        return isHolding;
    }

    // disables all holders
    public void DisableHold()
    {
        holdReady = false;
        foreach (var style in chargeStyles) animator.SetBool(style.ToString(), false);
    }

    // sets hold not ready to false (if no hold states are currently active)
    // this function is called when idle animation starts, so in order to avoid
    // the case where the idle animation loops back around after setting a hold animation
    // to true (therefore setting hold ready to false after the hold animation sets it to true)
    private void HoldNotReady()
    {
        if (!IsHolding()) {
            holdReady = false;
        }
    }

    public void SetHolder(ChargeStyles state, bool value)
    {
        // disable all other states. can only hold one at a time
        foreach (var style in chargeStyles)
        {
            if (style != state)
            {
                animator.SetBool(style.ToString(), false);
            }
        }

        switch (state)
        {
            case ChargeStyles.TwoFingerHold:
                animator.SetBool(TwoFingerHoldBool, value);
                break;

            case ChargeStyles.OpenPalmHold:
                animator.SetBool(OpenPalmHoldBool, value);
                break;
            
            case ChargeStyles.ClosedPalmHold:
                animator.SetBool(ClosedPalmHoldBool, value);
                break;

            default:
                Debug.LogError("Invalid hold state: " + state);
                break;
        }
    }

    // Sets cast value for a given cast style. If cast is a trigger, sets trigger.
    public void SetCast(CastStyles state, bool value)
    {
        // disable all other states. can only cast one at a time
        foreach (var style in castStyles)
        {
            if (style != state)
            {
                animator.SetBool(style.ToString(), false);
            }
        }

        switch (state)
        {
            case CastStyles.CastForward:
                animator.SetBool(CastForwardBool, value);
                break;
            
            case CastStyles.CastThrow:
                animator.SetTrigger(CastThrowTrigger);
                break;
        }
    }

    public void InitiateCastIfHolding(CastStyles style)
    {
        if (IsHolding())
        {
            SetCast(style, true);
        }
    }

    public void InitiateCastIfHolding(CastStyles style, ChargeStyles holdState)
    {
        if (IsHolding(holdState))
        {
            SetCast(style, true);
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        
        foreach (var finger in armTransforms) {
            if (finger.Value) {
                Gizmos.DrawSphere(finger.Value.position, 0.01f);
            }
            else InitializeFingers();
        }
    }

    private void Cast()
    {
        Cast(true);
    }

    // Triggers event
    private void Cast(bool disableHoldAfterCast = true) {
        OnCast?.Invoke();
        if (disableHoldAfterCast) DisableHold();
    }

    private void HoldIsReady() {
        holdReady = true;
        OnHoldReady?.Invoke();
    }

}
