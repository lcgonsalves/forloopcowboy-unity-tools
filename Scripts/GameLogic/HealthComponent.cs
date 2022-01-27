#nullable enable
using System;
using BehaviorDesigner.Runtime;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class HealthComponent : SerializedMonoBehaviour, IHasHealth, IManagedGameObject
    {
        public event Action? onDeath;
        
        /// <summary>
        /// When health component loses health, emits the damage amount and a damage provider, if one was provided.
        /// </summary>
        public event Action<int, IDamageProvider?>? onDamage;
        
        [SerializeField, Core.ReadOnly]
        private int health = 100;

        [SerializeField]
        private int _maxHealth = 100;
        public int MaxHealth => _maxHealth;

        [Tooltip("Game Objects with the health component and that are spawned using the UnitManager will be destroyed this many seconds after their health reaches zero.")]
        public float gameObjectDestroyDelay = 30f;
        
        public int Health
        {
            get => health;
            set
            {
                var clampedValue = Mathf.Clamp(value, 0, MaxHealth);
                var previousHealth = health;
                
                // set now so event is called with the health updated
                health = clampedValue;

                // only death event if dead AND WAS NOT DEAD BEFORE
                if (health == 0 && health < previousHealth)
                {
                    // if for some reason the character wakes up already dead, this will never trigger the destruction
                    // signal to the unit manager.
                    // if you want the corpse to be picked up by the garbage collection system, but the default
                    // function was never invoked, then you must run [[SetDestroyFlagWithDelay]] so the GC can properly.
                    // if the intention was to spawn a dead character, this behavior us to prevent the system from
                    // forcing a deletion when unintended.
                    onDeath?.Invoke();
                }
            }
        }

        private void Awake()
        {
            SetMaxHealth(MaxHealth);
            AttachOnDeathListeners();
        }

        public void SetMaxHealth(int newValue){ _maxHealth = newValue; health = _maxHealth; }
        
        private void AttachOnDeathListeners()
        {
            onDeath += OnDeathCleanup;
            onDeath += SetDestroyFlagWithDelay;
        }

        public void OnDeathCleanup()
        {
            // currently supports 3 death animations
            int animidx = Random.Range(1, 4);
            
            if (TryGetComponent(out Animator animator)) animator.SetInteger(DeathAnimationIndex, animidx);
            if (TryGetComponent(out AimComponent component)) component.StopTracking();
            if (TryGetComponent(out AdvancedNavigation navigation)) navigation.StopAndDisable();
            if (TryGetComponent(out WeaponUser weaponUser)) weaponUser.CeaseFire();

            foreach (var behavior in GetComponents<BehaviorTree>()) behavior.enabled = false;

            var ragdollComponent = GetComponent<Ragdoll>();
            if (ragdollComponent) ragdollComponent.RunAsyncWithDelay(2.5f, () => ragdollComponent.EnableRagdoll());

        }
        
        public void SetDestroyFlagWithDelay()
        {
            this.RunAsyncWithDelay(gameObjectDestroyDelay, () =>
            {
                _shouldDestroy = true;
            });
        }

        public bool IsAlive => Health > 0;

        public bool IsDead => !IsAlive;
        
        
        public void Damage(int amount, IDamageProvider damageSource)
        {
            var previousHealth = Health;
            Health -= amount;
            
            if (previousHealth > 0 && amount > 0) OnOnDamage(amount, damageSource);
        }

        [Button]
        public void Damage(int amount)
        {
            // invoked event expects source to be possibly null
            Damage(amount, null!);
        }

        [Button]
        public void Heal(int amount) { Health += amount; }

        private bool _shouldDestroy = false;
        private static readonly int DeathAnimationIndex = Animator.StringToHash("DeathAnimationIndex");

        public bool ShouldDestroy()
        {
            // see AttachOnDeathListeners()
            return _shouldDestroy;
        }
        
        /// <summary>
        /// Gets health component either in object, or if the
        /// collider is a Ragdoll.Limb, then we look for the health
        /// component in the master of the puppet.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static HealthComponent GetHealthComponent(GameObject other)
        {
            var healthComponent = other.gameObject.GetComponent<HealthComponent>();
            if (!healthComponent)
            {
                // if no health component in collider itself, try to see if it's a limb
                // and then look for the component on the root of the object
                var limbComponent = other.gameObject.GetComponent<Ragdoll.Limb>();
                if (limbComponent)
                {
                    healthComponent = limbComponent.master.GetComponent<HealthComponent>();
                    // if still no health component, look in the parent.
                    if (!healthComponent)
                        healthComponent = limbComponent.master.transform.parent.GetComponent<HealthComponent>();
                }
            }

            return healthComponent;
        }

        private void OnDestroy()
        {
            onDeath -= SetDestroyFlagWithDelay;
        }

        private void OnDrawGizmos()
        {
            Handles.Label(transform.position + Vector3.up, $"Health [{Health}]");
        }

        [Button]
        public void AttachHitBox()
        {
            var hb = this.GetOrElseAddComponent<HitBox>();
            hb.healthComponent = this;
        }

        protected virtual void OnOnDamage(int dmgAmount, IDamageProvider? damageProvider = null)
        {
            onDamage?.Invoke(dmgAmount, damageProvider);
        }
    }
}