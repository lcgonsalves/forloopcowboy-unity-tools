using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public class SoldierBehaviourStateManager
{
    // Public defs

    public enum State
    {
        Idle,
        Aware,
        Moving,
        Aiming,
        TakingCover,
        Reloading,
        TakingDamage,
        Dying,
        Engage
    }

    public enum StateParameters
    {
        Velocity,
        YAimAngle,
        EnemySpotted,
        NoTargetToEngage,
        Engage,
        Die,
        Crouched
    }

    protected int BEHAVIOUR_STATE_LAYER;
    
    public const string BEHAVIOUR_STATE_LAYER_NAME = "Behaviour";
    public const string PROCESSOR_PREFIX = "Process";

    public State currentState { get; private set; }
    public State previousState { get; private set; }
    
    // Internal refs

    Animator animator;
    Dictionary<int, State> stateHashes = new Dictionary<int, State>();
    Dictionary<State, MethodInfo> stateProcessors = new Dictionary<State, MethodInfo>();
    SoldierBehaviour controller;

    public SoldierBehaviourStateManager(SoldierBehaviour soldierToControl) 
    {
        this.controller = soldierToControl;
        this.animator = soldierToControl.animator;

        if (soldierToControl == null || animator == null) throw new NullReferenceException("Soldier must exist and have an animator in order use the state manager.");

        BEHAVIOUR_STATE_LAYER = animator.GetLayerIndex(BEHAVIOUR_STATE_LAYER_NAME);

        // initialize hashes'
        foreach(State state in Enum.GetValues(typeof(State)))
        {
            stateHashes.Add(SoldierBehaviourStateManager.GenerateStateHash(state), state);
        }
    }

    // Static method generates the hash
    public static int GenerateStateHash(State state) { return Animator.StringToHash($"{BEHAVIOUR_STATE_LAYER_NAME}.{state.ToString()}"); }

    // Returns a state if a given hash has been mapped to the state.
    public State GetState(int hash)
    {
        State state;
        if (stateHashes.TryGetValue(hash, out state)) return state;
        else throw new System.ArgumentException("No States correspond to this hashPath. Please create one in the class.");
    }

    // Returns the method on this manager to process a given state, if one exists. Uses dictionary caching.
    public MethodInfo GetStateProcessor(State state)
    {
        MethodInfo methodInfo;
        if (stateProcessors.TryGetValue(state, out methodInfo)) return methodInfo;
        else {

            var methodName = $"{PROCESSOR_PREFIX}{state.ToString()}";

            // gets non public instance method with no params
            methodInfo = this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new Type[0], null);

            if (methodInfo != null) { stateProcessors.Add(state, methodInfo); return methodInfo; }
            else throw new System.ArgumentException($"The state {state.ToString()} does not possess a method to process it.");
        }
    }

    // Reads animator state in behaviour layer and calls the appropriate function
    // to process that state. If a state is called "Sample" then there should be a method called "ProcessSample" defined.
    public void ProcessState()
    {
        // first update the parameters
        WriteStateParameters();

        // then read the resulting state

        var stateInfo = animator.GetCurrentAnimatorStateInfo(BEHAVIOUR_STATE_LAYER);
        previousState = currentState;
        var state = currentState = GetState(stateInfo.fullPathHash);
        
        // invoke state processor for a given state
        GetStateProcessor(state)?.Invoke(this, new object[0]); // handlers do not take arguments
    }

    // Reads state of soldier and writes it to animator
    public void WriteStateParameters()
    {
        // if dead, set trigger and return (we dont want to continue tracking state at this point)
        if (controller.Health == 0 && currentState != State.Dying) {
            animator.SetTrigger(StateParameters.Die.ToString());
            return;
        }

        // update velocity
        animator.SetFloat(StateParameters.Velocity.ToString(), controller.currentVelocity);

        // update whether soldier has spotted someone or not when it has detected enemies
        animator.SetBool(StateParameters.EnemySpotted.ToString(), controller.spotted.Count > 0);

        // if current target is dead, announce it and reset target variable
        if (controller.targetSoldier && controller.targetSoldier.Health == 0)
        {
            animator.SetTrigger(StateParameters.NoTargetToEngage.ToString());
            controller.targetSoldier = null;
        }

        // ready to engage if we can find a spotted target that is alive (and we are not targeting anybody currently)
        if (controller.spotted.Count > 0 && controller.targetSoldier == null)
        {
            // select first target that has health
            foreach (var target in controller.spotted) { if (target.Health > 0) { controller.targetSoldier = target; break; } }

            // if we found a living target, proceed to engage otherwise 
            var trigger = controller.targetSoldier == null ? StateParameters.NoTargetToEngage.ToString() :  StateParameters.Engage.ToString();
            animator.SetTrigger(trigger);
        }

    }

    public void SetCrouch(bool value)
    {
        controller.animator.SetBool(SoldierBehaviourStateManager.StateParameters.Crouched.ToString(), value);
    }

    //// DEFINE HERE THE INDIVIDUAL STATE PROCESSORS ////

    private void ProcessIdle() 
    {
        // not aiming
        // waiting to spot a soldier
        controller.aim.StopTracking();
    }

    private void ProcessAware()
    {
        // soldier spotted
        // stop moving and track it
        var trgtSoldier = controller.targetSoldier;
        var visibleTarget = controller.firstAvailableTargetColliderInview; // might be performance critial

        if (trgtSoldier != null && visibleTarget != null) controller.aim.Track(visibleTarget);
    }

    private void ProcessMoving()
    {
        // stop aiming and shooting
    }

    private void ProcessEngage()
    {
        var targetSoldier = controller.targetSoldier;
        var visibleTarget = controller.firstAvailableTargetColliderInview;

        // track enemy and periodically shoot at it (in bursts, when i get this shit to work)
        if (targetSoldier && visibleTarget && targetSoldier.Health > 0) {
            controller.aim.Aim(visibleTarget.position, controller.weaponController.OpenFire);
        } else controller.weaponController.CeaseFire();

    }

    private void ProcessDying()
    {
        // stop everything and let death animation play
        controller.weaponController.CeaseFire();
        controller.aim.StopTracking();
        controller.StopAllCoroutines();

        // stop IK

        // drop weapon

        // ragdoll is triggered at the end of the animation
    }

}
