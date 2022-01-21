using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    [CreateAssetMenu]
    public class Bullet : ScriptableObject, IDamageProvider
    {
        public GameObject prefab;

        public GameObject onImpact;

        public float muzzleVelocity = 100f;

        public int maxBounces = 0;

        [Tooltip("in seconds")]
        public float lifetime = 10f;

        
        [Header("Damage Settings")]
        public int minDamage = 1;
        public int maxDamage = 10;
        public int defaultDamage = 5;

        [Header("Pooling settings")]
        [Tooltip("Number of bullets kind that are spawned before previous bullet is recycled.")]
        public int maximumConcurrentSpawnedBullets = 10;
        
        [Range(0f, 1f)]
        public float bias = 0f;

        public enum DamageMode
        {
            RandomBetweenMinAndMax,
            BiasTowardsMin,
            BiasTowardsMax,
            BiasTowardsDefault,
            Static
        }

        public DamageMode damageMode;

        [ContextMenu("Auto-correct damage values")]
        public void CorrectDamages()
        {
            var min = Mathf.Min(minDamage, maxDamage, defaultDamage);
            var max = Mathf.Max(minDamage, maxDamage, defaultDamage);
            var l = new List<int>(new[] {minDamage, maxDamage, defaultDamage});
            l.Remove(min);
            l.Remove(max);

            defaultDamage = l[0];
            minDamage = min;
            maxDamage = max;

        }
        
        public int GetDamageAmount()
        {

            switch (damageMode)
            {
                case DamageMode.RandomBetweenMinAndMax:
                    return Random.Range(minDamage, maxDamage);
                case DamageMode.BiasTowardsMin:
                    return Mathf.RoundToInt(Mathf.Lerp(Random.Range(minDamage, maxDamage), minDamage, bias));
                case DamageMode.BiasTowardsMax:
                    return Mathf.RoundToInt(Mathf.Lerp(Random.Range(minDamage, maxDamage), maxDamage, bias));
                case DamageMode.BiasTowardsDefault:
                    return Mathf.RoundToInt(Mathf.Lerp(Random.Range(minDamage, maxDamage), defaultDamage, bias));
                case DamageMode.Static:
                    return defaultDamage;
                default:
                    return defaultDamage;
            }
        }
    }
}
