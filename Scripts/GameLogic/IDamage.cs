using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    public interface IDamageProvider
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
    
    public class DamageSystem { public static string tag = "IDamaging"; }

}