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