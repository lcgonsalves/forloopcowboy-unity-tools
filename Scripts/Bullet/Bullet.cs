using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.Bullet
{
    [CreateAssetMenu]
    public class Bullet : SerializedScriptableObject, IDamageProvider
    {
        [TabGroup("Prefab")]
        public GameObject prefab;
        
        [TabGroup("Prefab")]
        [FormerlySerializedAs("onImpact")] public GameObject onImpactPrefab;

        [TabGroup("Prefab")]
        public bool spawnOnFirstImpact = false;
        [TabGroup("Prefab")]
        public bool spawnOnImpact = true;
        [FormerlySerializedAs("spawnOnLastImpact")] [TabGroup("Prefab")]
        public bool spawnOnFinalImpact = false;

        [TabGroup("Physics")]
        public float muzzleVelocity = 100f;

        [TabGroup("Physics")]
        public int maxBounces = 0;

        [Tooltip("in seconds")] [TabGroup("Pooling")]
        public float lifetime = 10f;

        
        [TabGroup("Damage")]
        [Header("Damage Settings")]
        public int minDamage = 1;
        [TabGroup("Damage")]
        public int maxDamage = 10;
        [TabGroup("Damage")]
        public int defaultDamage = 5;
        [TabGroup("Damage")]
        [Range(0f, 1f)]
        public float bias = 0f;
        [TabGroup("Damage")]
        public DamageMode damageMode;

        [TabGroup("Pooling")]
        [Header("Pooling settings")]
        [Tooltip("Number of bullets kind that are spawned before previous bullet is recycled.")]
        public int maximumConcurrentSpawnedBullets = 10;

        public enum DamageMode
        {
            RandomBetweenMinAndMax,
            BiasTowardsMin,
            BiasTowardsMax,
            BiasTowardsDefault,
            Static
        }

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
