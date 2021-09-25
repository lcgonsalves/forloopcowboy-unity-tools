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
            onDeath += () =>
            {
                this.RunAsyncWithDelay(gameObjectDestroyDelay, () =>
                {
                    _shouldDestroy = true;
                });
            };
        }

        public bool IsAlive => Health > 0;

        public bool IsDead => !IsAlive;
        
        public void Damage(int amount) { HasHealthDefaults.Damage(this, amount); }

        public void Heal(int amount) { HasHealthDefaults.Heal(this, amount); }

        private bool _shouldDestroy = false;
        public bool ShouldDestroy()
        {
            // see AttachOnDeathListeners()
            return _shouldDestroy;
        }
    }
}