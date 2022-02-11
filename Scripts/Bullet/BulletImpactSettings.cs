using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    [CreateAssetMenu(fileName = "Untitled Bullet Impact Settings", menuName = "Settings.../New Bullet Impact Settings", order = 0)]
    public class BulletImpactSettings : SerializedScriptableObject
    {
        [Tooltip("Whenever a bullet of the given kind collides with this collider, the gameobject is spawned and set to destroy after X seconds.")]
        public Dictionary<Bullet, GameObject> particlesForBullet = new Dictionary<Bullet, GameObject>();

        public GameObject defaultPrefab = null;

        public bool TryGetParticleFor(Bullet bullet, out GameObject particle)
        {
            if (particlesForBullet.TryGetValue(bullet, out var specificParticle))
            {
                particle = specificParticle;
                return true;
            }
            else if (defaultPrefab)
            {
                particle = defaultPrefab;
                return true;
            }
            else
            {
                particle = null;
                return false;
            }
        }

    }
}