using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [CreateAssetMenu]
    public class Soldier : ScriptableObject
    {
        public GameObject prefab;

        public Weapon.Weapon weapon;

        public float visibilityRange = 10f;

        // how many seconds does it take to aim
        public Transition easeToAimTransition;
        public Transition aimToEaseTransition;

    }
}
