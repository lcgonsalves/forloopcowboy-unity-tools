using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [CreateAssetMenu]
    public class Soldier : ScriptableObject
    {
        public Weapon.Weapon startingWeapon;

        public float visibilityRange = 10f;

        // how many seconds does it take to aim
        public Transition easeToAimTransition;
        public Transition aimToEaseTransition;

    }
}
