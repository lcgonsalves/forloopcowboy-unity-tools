using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Player;
using forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Misc
{
    public class SpawnBarrierSpell : Spell
    {
        private Camera mainCam = null;

        public string initialRootName = "InitialRoot";
        public string shatteredRootName = "ShatteredRoot";

        [Tooltip("When shattered barrier object is at X percent built, it begins to protect against impact.")]
        public float readyAtPercent = 0.5f;
        
        /// <summary>
        /// Spawns the main effect, which should be shaped as such
        /// Object
        ///   > InitialRoot - fully built barrier
        ///   > ShatteredRoot - destroyed barrier
        ///
        /// We will lerp from shattered to initial when casting gradually.
        ///
        /// </summary>
        protected override void Execute(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            mainCam = mainCam ? mainCam : Camera.main;
            
            
        }

        private void OnValidate()
        {
            var initialRoot = mainEffect.transform.FindRecursively(t => t.name == initialRootName);
            var shatteredRoot = mainEffect.transform.FindRecursively(t => t.name == shatteredRootName);

            if (!initialRoot) throw new Exception($"No initial root defined. Make sure your mainEffect has a child object named {initialRoot}");
            if (!shatteredRoot) throw new Exception($"No shattered root defined. Make sure your mainEffect has a child object named {shatteredRoot}");
            
            // check that each shattered particle has a counterpart.
            for (int shatterParticle_i = 0; shatterParticle_i < shatteredRoot.childCount; shatterParticle_i++)
            {
                var shatterParticle = shatteredRoot.GetChild(shatterParticle_i);
                if (initialRoot.transform.FindRecursively(t => t.name == shatterParticle.name) == null) 
                    throw new Exception($"No initial particle found for {shatterParticle.name}");
            }

        }

        [MenuItem("Spells/New.../Barrier")]
        static void CreateBulletSpell(){ Spell.CreateSpell<SpawnBarrierSpell>("Barrier"); }
    }
}