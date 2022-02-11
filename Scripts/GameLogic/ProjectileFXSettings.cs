using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    [CreateAssetMenu(fileName = "Untitled FX Settings", menuName = "Settings.../Projectile FX", order = 0)]
    public class ProjectileFXSettings : ScriptableObject
    {
        public List<ProjectileFX> effects;

        public IEnumerable<ProjectileFX> FirstImpactFX => effects.Where(fx => fx.spawnOnFirstImpact);
        public IEnumerable<ProjectileFX> ImpactFX => effects.Where(fx => fx.spawnOnImpact);
        public IEnumerable<ProjectileFX> LastImpactFX => effects.Where(fx => fx.spawnOnLastImpact);
        public IEnumerable<ProjectileFX> FireFX => effects.Where(fx => fx.spawnOnFire);
        public IEnumerable<ProjectileFX> DeathFX => effects.Where(fx => fx.spawnOnDeath);
    }

    [Serializable]
    public struct ProjectileFX
    {
        public GameObject prefab;
        public FXDespawnSettings despawnSettings;
        
        public bool spawnOnFirstImpact, spawnOnImpact, spawnOnLastImpact, spawnOnFire, spawnOnDeath;
        public float velocitySpawnThreshold;

        public bool SpawnOnAnyImpact => spawnOnFirstImpact || spawnOnImpact || spawnOnLastImpact;
        
        [ShowIf("SpawnOnAnyImpact")]
        public bool orientToCollisionNormal;
    }

    [Serializable]
    public struct FXDespawnSettings
    {
        public bool destroyAutomatically;
        
        [ShowIf("destroyAutomatically")]
        public float destroyDelay;
    }
}