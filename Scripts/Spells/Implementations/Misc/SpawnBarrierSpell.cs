using System;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
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

        [UnityEngine.Tooltip("When shattered barrier object is at X percent built, it begins to protect against impact.")]
        public float readyAtPercent = 0.5f;

        [UnityEngine.Tooltip("Number of hits that a full shield can survive.")]
        public int strength = 1;
        
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
            var castPosition = GetTargetPosition(caster);

            var barrierInstance = Instantiate(mainEffect, castPosition, caster.transform.rotation);

            if (barrierInstance.TryGetComponent(out SimpleReconstructLerp lerp))
            {
                lerp.Initialize();
                lerp.SpawnGraduallyAndLerpToDestination(SimpleReconstructLerp.Position.Initial);
                
                var detector = lerp.GetOrElseAddComponent<CollisionDetector>();
                
                detector.Initialize();
                detector.onCollision += OnBarrierParticleCollision(detector, lerp);
            }
        }

        /// <summary>
        /// When a particle collides with something during
        /// its lerp, and the shield isn't solid enough,
        /// all particles break away.
        /// If the shield is solid, and the particle is lerping, only the particle activates.
        /// If the particle is part of the shield, it increments a hit count. If the hit count
        /// reaches above the threshold, the whole shield breaks away.
        /// </summary>
        /// <param name="master"></param>
        /// <param name="lerp"></param>
        /// <returns></returns>
        private Action<CollisionDetector, Collision> OnBarrierParticleCollision(CollisionDetector master, SimpleReconstructLerp lerp)
        {
            int hitCount = 0;
            
            return (detector, collision) =>
            {
                SimpleReconstructLerp.Position shieldPosition = SimpleReconstructLerp.Position.Final; // editor setting

                // only handle collisions with bullets
                if (collision.transform.GetComponent<BulletController>() == null)
                {
                    return;
                }
                
                bool isShieldSolidEnough = lerp.percentInPlace >= readyAtPercent;

                hitCount++;

                if (!isShieldSolidEnough || hitCount == strength)
                {
                    BlowItUp(master, lerp, collision.transform);
                }
            };
        }

        private void BlowItUp(CollisionDetector master, SimpleReconstructLerp lerp, Transform impactPosition)
        {
            master.onCollision -= OnBarrierParticleCollision(master, lerp);
            
            lerp.InterruptAll();
            
            foreach (var cachedParticle in lerp.cachedParticles)
            {
                var rb = cachedParticle.GetComponentInChildren<Rigidbody>();
                lerp.StopAndEnablePhysics(cachedParticle);
            
                // todo: bullet / spell defines explosion force?
                float force = 140f, radius = 1.5f;

                if (rb)
                {
                    rb.AddExplosionForce(force, impactPosition.transform.position, radius);
                }
            }
            
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