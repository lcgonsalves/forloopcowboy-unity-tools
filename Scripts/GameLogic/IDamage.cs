using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ForLoopCowboyCommons.EditorHelpers;

namespace ForLoopCowboyCommons.Damage
{
    public interface IDamaging
    {
        // Returns the amount of damage the object should do
        int GetDamageAmount();
        
    }
    
    public interface IHasHealth
    {
        Transform transform { get; }
        int MaxHealth { get; }
        int Health { get; set; }
        bool IsAlive { get; }
        
        bool IsDead { get; }
        
        public void Damage(int amount);
        public void Heal(int amount);

    }

    public static class HasHealthDefaults
    {
        public static void Damage(this IHasHealth self, int amount) { self.Health = Mathf.Clamp(self.Health - amount, 0, self.MaxHealth); }

        public static void Heal(this IHasHealth self, int amount) { self.Health = Mathf.Clamp(self.Health + amount, 0, self.MaxHealth); }
    }

    public class DamageSystem { public static string tag = "IDamaging"; }

    /// Damage providers are tagged with "IDamaging" in the game
    public class SimpleDamageProvider : MonoBehaviour, IDamaging
    {
        public int max = 2;
        public int min = 1;


        public int GetDamageAmount()
        {
            if (max < min) throw new System.Exception("Min cannot be higher than max");

            return Random.Range(min, max);
        }

        private void OnEnable() {
            this.gameObject.tag = DamageSystem.tag;
        }

    }

}