using UnityEngine;

[CreateAssetMenu]
public class Soldier : ScriptableObject
{
    public GameObject prefab;

    public Weapon weapon;

    public float visibilityRange = 10f;

    // how many seconds does it take to aim
    public Transition easeToAimTransition;
    public Transition aimToEaseTransition;

}
