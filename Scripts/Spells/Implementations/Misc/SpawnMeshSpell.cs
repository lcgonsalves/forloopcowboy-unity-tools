using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityString;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile;
using RayFire;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Misc
{
    public class SpawnMeshSpell : Spell
    {
        private Camera mainCam = null;

        [FoldoutGroup("Mesh Spell Settings")]
        public string initialRootName = "InitialRoot";
        
        [FoldoutGroup("Mesh Spell Settings")]
        public string shatteredRootName = "ShatteredRoot";

        [FoldoutGroup("Mesh Spell Settings")]
        [UnityEngine.Tooltip("When shattered barrier object is at X percent built, it begins to protect against impact.")]
        public float readyAtPercent = 0.5f;

        [FoldoutGroup("Mesh Spell Settings")]
        [UnityEngine.Tooltip("Number of hits that a full shield can survive.")]
        public int strength = 1;
        
        [FoldoutGroup("Mesh Spell Settings")] public float selfDestructAfterSeconds = 5f;
        
        
        /// <summary>
        /// Gets the target position using a boxcast with the BoundingBox specs instead.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="mainCamera">If none is provided, uses Camera.main</param>
        /// <returns></returns>
        public override Vector3 GetTargetPosition(SpellUserBehaviour caster = null, Camera mainCamera = null)
        {
            if (showPreviewOnCastTarget && caster && caster.GetTarget(this, out var target)) return target.transform.position;

            mainCamera = mainCamera ? mainCamera : Camera.main;
            
            Assert.IsNotNull(mainCamera, "Must define a main camera.");

            // cast a ray forward and if it hits anything, that's the target regardless of the style
            var centerOfScreen = new Vector3(Screen.width / 2f, Screen.height / 2f, mainCamera.nearClipPlane);
            Vector3 centerOfScreenWrld = mainCamera.ScreenToWorldPoint(centerOfScreen);
            Ray forward = mainCamera.ScreenPointToRay(centerOfScreen);
            
            var bb = mainEffect.transform.FindRecursively(_ => _.name == "BoundingBox");
            Vector3 boundingBoxExtents = Vector3.zero;
            Vector3 boundingBoxCenterPosition = bb?.localPosition ?? Vector3.zero;

            if (bb && bb.TryGetComponent(out BoxCollider c))
            {
                var size = c.size;
                var localScale = c.transform.localScale;
                
                boundingBoxExtents = new Vector3(
                    size.x * localScale.x,
                    size.y * localScale.y,
                    size.z * localScale.z
                );
            }

            if (Physics.BoxCast(
                centerOfScreenWrld,
                boundingBoxExtents / 2,
                forward.direction,
                out var hit,
                caster ? caster.transform.rotation : Quaternion.identity,
                range, 
                raycastLayer
            ))
            {
                var endpoint = mainCamera.transform.position + forward.direction * hit.distance;
                
                GlobalGizmoDrawer.CustomGizmo(
                    "BoxProjection",
                    () =>
                    {
                        Gizmos.color = Color.blue;

                        Gizmos.DrawLine(mainCamera.transform.position, endpoint);
                        
                        Gizmos.matrix = caster.transform.localToWorldMatrix;
                        var correctedEndpoint = caster.transform.InverseTransformPoint(endpoint);
                        
                        Gizmos.DrawWireCube(correctedEndpoint, boundingBoxExtents);
                    }
                );
                
                
                
                // accounts for bounding extents to spawn
                // accounts for bounding box's extents so shape is in the middle
                // and it makes it easier to set them up in the editor
                return endpoint + boundingBoxCenterPosition;
            }
            else
            {
                var endpoint = mainCamera.ScreenToWorldPoint(centerOfScreen) + (forward.direction.normalized * range);
                
                GlobalGizmoDrawer.CustomGizmo(
                    "BoxProjection",
                    () =>
                    {
                        Gizmos.color = Color.red;

                        Gizmos.DrawLine(mainCamera.transform.position, endpoint);
                        
                        Gizmos.matrix = caster.transform.localToWorldMatrix;
                        var correctedEndpoint = caster.transform.InverseTransformPoint(endpoint);
                        
                        Gizmos.DrawWireCube(correctedEndpoint, boundingBoxExtents);
                    }
                );
            }

            if (targetingStyle == TargetingStyle.Ranged)
            {
                // project a point range meters away in the direction of the camera
                return mainCamera.ScreenToWorldPoint(centerOfScreen) + (forward.direction.normalized * range);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Spawns the main effect, which should be shaped as such
        /// Object
        ///   > InitialRoot - fully built barrier
        ///   > ShatteredRoot - destroyed barrier
        ///   > BoundingBox - defines the borders of the object for proper raycasting
        ///
        /// We will lerp from shattered to initial when casting gradually.
        ///
        /// </summary>
        protected override void Execute(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            // barrier can never be targeted
            showPreviewOnCastTarget = false;
            var castPosition = GetTargetPosition(caster);

            var barrierInstance = Instantiate(mainEffect, castPosition, caster.transform.rotation);

            if (barrierInstance.TryGetComponent(out SimpleReconstructLerp lerp))
            {
                lerp.Initialize();
                
                var detector = lerp.GetOrElseAddComponent<CollisionDetector>();
                
                detector.Initialize();
                detector.onCollision += OnBarrierParticleCollision(detector, lerp);
                
                lerp.SpawnGraduallyAndLerpToDestination(
                    SimpleReconstructLerp.Position.Initial,
                    // begins self destruction once all particles have been SPAWNED
                    // you must still account for the lerp to position time, this is the simple
                    // solution i felt like implementing rn 
                    () => 
                    {
                        lerp.RunAsyncWithDelay(
                            selfDestructAfterSeconds,
                            () =>
                            {
                                detector.onCollision -= OnBarrierParticleCollision(detector, lerp);
                                BlowItUp(detector, lerp, barrierInstance.transform);
                            }
                        );
                    }
                );
            }
        }

        public override void Preview(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            if (caster.ParticleInstancesFor(this, source, out var particles) && particles.targetPreview)
            {
                var targetPreview = particles.targetPreview;
                bool wasActive = targetPreview.activeSelf && targetPreview.activeInHierarchy;
                base.Preview(caster, source, direction);
                bool isActive = targetPreview.activeSelf && targetPreview.activeInHierarchy;

                if (!wasActive && isActive && targetPreview.TryGetComponent(out SimpleReconstructLerp lerp))
                {
                    lerp.InitializeIfNeeded();
                    lerp.ResetObjects();
                    lerp.SpawnGradually(SimpleReconstructLerp.Position.Initial, .15f);
                }
            }
        }

        /// <summary>
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

                if (detector.TryGetComponent(out SimpleReconstructLerp.ReconstructLerpSlave lerpSlave))
                {
                    lerpSlave.StopCurrent();
                }
                
                
                if (detector.c is { } && detector.c.TryGetComponent(out RayfireRigid rigid))
                {
                    Debug.Log($"Impact on rayfire rigid. Hit count {hitCount}/{strength}"); 
                    
                    rigid.Initialize();
                    rigid.Activate();
                    rigid.Demolish();
                    
                    float impactForce = 20f;
                    var contact = collision.GetContact(0);

                    var rb = rigid.GetComponent<Rigidbody>();
                    if (rb) rb.AddExplosionForce(impactForce, contact.point, 1.5f);
                }
                
                // if is a slave, we check if it is lerping
                // collision with loose lerping particles shouldn't blow up the mesh
                bool isSlaveAndIslerping = lerpSlave != null && lerpSlave.isLerping;
                
                if (!isSlaveAndIslerping && !isShieldSolidEnough || hitCount == strength)
                {
                    BlowItUp(master, lerp, collision.transform);
                }
                else hitCount++;
            };
        }

        private void BlowItUp(CollisionDetector master, SimpleReconstructLerp lerp, Transform impactPosition)
        {
            master.onCollision -= OnBarrierParticleCollision(master, lerp);
            
            lerp.InterruptAll();

            if (lerp.cachedParticles != null)
            {
                bool hasExploded = false;
                var rigidbodies = new List<Rigidbody>(lerp.cachedParticles.Length);
                
                foreach (var cachedParticle in lerp.cachedParticles)
                {
                    try
                    {
                        // particle has already been destroyed by another process.
                        if (cachedParticle == null) continue;
                    
                        var rb = cachedParticle.GetComponentInChildren<Rigidbody>();
                        lerp.StopAndEnablePhysics(cachedParticle);
                        
                        Destroy(cachedParticle, UnityEngine.Random.Range(5f, 10f));

                        if (rb)
                        {
                            rigidbodies.Add(rb);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Caught an exception while trying to disable and explode: {e.Message}");
                    }
                }

                if (!rigidbodies.IsNullOrEmpty())
                {
                    // todo: bullet / spell defines explosion force?
                    float force = 150f, radius = 1.5f;
                    
                    var xploderb = rigidbodies[UnityEngine.Random.Range(0, rigidbodies.Count)];
                    xploderb.AddExplosionForce(force, impactPosition.transform.position, radius);
                    
                }

                lerp.ResetInitialization(false);

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
        static void CreateBulletSpell(){ Spell.CreateSpellAsset<SpawnMeshSpell>("Barrier"); }
    }
}