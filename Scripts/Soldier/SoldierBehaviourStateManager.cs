using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    public class SoldierBehaviourStateManager
    {
        // Public defs

        public enum State
        {
            Ready,
            Aware,
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
        SoldierBehaviour soldier;

        public SoldierBehaviourStateManager(SoldierBehaviour soldierToControl) 
        {
            this.soldier = soldierToControl;
            this.animator = soldierToControl.animator;

            if (soldierToControl == null || animator == null) throw new NullReferenceException("Soldier must exist and have an animator in order use the state manager.");

            BEHAVIOUR_STATE_LAYER = animator.GetLayerIndex(BEHAVIOUR_STATE_LAYER_NAME);
        
            // initialize hashes'
            foreach(State state in Enum.GetValues(typeof(State)))
            {
                stateHashes.Add(GenerateStateHash(state), state);
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
            GetStateProcessor(state)?.Invoke(this, Array.Empty<object>()); // handlers do not take arguments
        }

        // Reads state of soldier and writes it to animator
        public void WriteStateParameters()
        {
            // if dead, set trigger and return (we dont want to continue tracking state at this point)
            if (soldier.health.IsDead && currentState != State.Dying) {
                animator.SetTrigger(StateParameters.Die.ToString());
                return;
            }

            // update whether soldier has spotted someone or not when it has detected enemies
            animator.SetBool(StateParameters.EnemySpotted.ToString(), soldier.spotted.Count > 0);

            // if current target is dead, announce it and reset target variable
            if (soldier.target is {IsDead: true})
            {
                animator.SetTrigger(StateParameters.NoTargetToEngage.ToString());
                soldier.target = null;
            }
        
            // ready to engage if we can find a spotted target that is alive (and we are not targeting anybody currently)
            if (soldier.spotted.Count > 0)
            {
                // select first target that has health
                foreach (var target in soldier.spotted)
                {
                    if (target.IsAlive) { soldier.target = target; break; }
                }
            
                // if we found a living target, proceed to engage otherwise
                var trigger = soldier.target == null ? StateParameters.NoTargetToEngage.ToString() :  StateParameters.Engage.ToString();
                animator.SetTrigger(trigger);
            }
            else
            {
                // reset engagement when no targets
                animator.SetTrigger(StateParameters.NoTargetToEngage.ToString());
            
                // resume trajectory if one was started
                if (soldier.navigation.IsAbleToResumeNavigating()) soldier.navigation.Resume();
            
                // update velocity if no soldiers are spotted.
                animator.SetFloat(StateParameters.Velocity.ToString(), soldier.currentVelocity);
            }

        }

        public void SetCrouch(bool value)
        {
            soldier.animator.SetBool(SoldierBehaviourStateManager.StateParameters.Crouched.ToString(), value);
        }

        //// DEFINE HERE THE INDIVIDUAL STATE PROCESSORS ////

        private void ProcessReady() 
        {
            // not aiming
            // waiting to spot a soldier
            soldier.aim.StopTracking();
        }

        private void ProcessAware()
        {
            // soldier spotted
            // stop moving and track it
            var trgtSoldier = soldier.target;
            var visibleTarget = soldier.firstAvailableTargetColliderInView; // might be performance critial

            if (soldier.navigation.IsFollowingPath()) soldier.navigation.Pause();

            if (trgtSoldier != null && visibleTarget != null) soldier.aim.Track(visibleTarget);
        }

        private void ProcessMoving()
        {
            // stop aiming and shooting
        }

        private void ProcessEngage()
        {
            var targetSoldier = soldier.target;
            var visibleTarget = soldier.firstAvailableTargetColliderInView;

            // track enemy and periodically shoot at it (in bursts, when i get this shit to work)
            if (targetSoldier != null && visibleTarget != null && targetSoldier.Health > 0) {
                // soldier.aim.DeprecatedAim(visibleTarget.position, () => soldier.weaponController.OpenFire(true));
            } else soldier.weaponController.CeaseFire();

        }

        private void ProcessDying()
        {
            // stop everything and let death animation play
            soldier.weaponController.CeaseFire();
            soldier.aim.StopTracking();
            soldier.StopAllCoroutines();
            soldier.navMeshAgent.enabled = false;

            // stop IK

            // drop weapon

            // ragdoll is triggered at the end of the animation
        }

    }
}
