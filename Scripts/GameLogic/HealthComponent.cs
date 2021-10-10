using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public class HealthComponent : MonoBehaviour, IHasHealth, IManagedGameObject
    {
        public event Action onDeath;
        
        [SerializeField, ReadOnly]
        private int health = 100;

        [SerializeField]
        private int _maxHealth = 100;
        public int MaxHealth => _maxHealth;

        [Tooltip("Game Objects with the health component and that are spawned using the UnitManager will be destroyed this many seconds after their health reaches zero.")]
        public float gameObjectDestroyDelay = 5f;
        
        public int Health
        {
            get => health;
            set
            {
                health = Mathf.Clamp(value, 0, MaxHealth);
                if (health == 0) onDeath?.Invoke();
            }
        }

        private void Start()
        {
            AttachOnDeathListeners();
        }

        private void AttachOnDeathListeners()
        {
            onDeath += DelayDestroyOnDeath;
        }

        private void DelayDestroyOnDeath()
        {
            this.RunAsyncWithDelay(gameObjectDestroyDelay, () =>
            {
                _shouldDestroy = true;
            });
        }

        public bool IsAlive => Health > 0;

        public bool IsDead => !IsAlive;
        
        public void Damage(int amount) { Health -= amount; }

        public void Heal(int amount) { HasHealthDefaults.Heal(this, amount); }

        private bool _shouldDestroy = false;
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
            onDeath -= DelayDestroyOnDeath;
        }

        public static HealthComponent GetHealthComponent(Component other) { return GetHealthComponent(other); }
    }
}