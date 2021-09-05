using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    [CreateAssetMenu]
    public class Bullet : ScriptableObject
    {
        public GameObject prefab;

        public GameObject onImpact;

        public float muzzleVelocity = 100f;

        public int maxBounces = 0;

        [Tooltip("in seconds")]
        public float lifetime = 10f;

    }
}
