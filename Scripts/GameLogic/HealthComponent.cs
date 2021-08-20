using System;
using ForLoopCowboyCommons.EditorHelpers;
using UnityEngine;

namespace ForLoopCowboyCommons.Damage
{
    public class HealthComponent : MonoBehaviour, IHasHealth
    {
        public event Action onDeath;
        
        [SerializeField, ReadOnly]
        private int health = 100;

        [SerializeField]
        private int _maxHealth = 100;
        public int MaxHealth => _maxHealth;

        public int Health
        {
            get => health;
            set
            {
                health = Mathf.Clamp(value, 0, MaxHealth);
                if (health == 0) onDeath?.Invoke();
            }
        }

        public bool IsAlive => Health > 0;

        public bool IsDead => !IsAlive;
        
        public void Damage(int amount) { HasHealthDefaults.Damage(this, amount); }

        public void Heal(int amount) { HasHealthDefaults.Heal(this, amount); }
        
    }
}