using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using RayFire;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor.UIElements;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile
{
    /// <summary>
    /// To be added to the bullet which is the black hole component.
    /// All objects within radius will use physics to be pulled towards the
    /// black hole center.
    /// Integrates rayfire activation and initialization.
    /// Exposes a static method for exposing the functionality
    /// to any component with a collider.
    /// </summary>
    public class BlackHoleSuctionComponent : SerializedMonoBehaviour
    {
        public static readonly float ExplosionDuration = 1.35f; // magic number from the particle animation, idk how to fetch that dynamically
        
        public bool showPreview = true;
        public LayerMask suckingLayer;
        public Force.Settings forceSettings = new Force.Settings(.15f, 10f);
        public float suckRadius = 1f; 
        public float stopRadius => forceSettings.stopRadius;

        [Tooltip("If one is provided or attached to this object, will control bullet movement on impact.")]
        public BulletController bulletController;

        public bool suckTowardsCenter = false;
        
        [ShowInInspector]
        Collider[] _attractedObjectBuffer = new Collider[15];
        
        private void Awake()
        {
            if (bulletController == null) bulletController = GetComponent<BulletController>();

            InitializeBulletControllerReactivity();
        }

        private void InitializeBulletControllerReactivity()
        {
            if (bulletController == null) return;

            var bulletControllerCollider = bulletController.rb.GetComponent<Collider>();
            
            bulletController.onFirstImpact += collision =>
            {
                bulletController.rb.velocity = Vector3.zero;
                bulletController.rb.isKinematic = true;
                
                SuckEverythingWithinRadius(
                    bulletControllerCollider,
                    suckRadius,
                    stopRadius,
                    forceSettings.forceMagnitude,
                    suckingLayer,
                    _attractedObjectBuffer
                );

                // Will disable by time if it doesn't disable by sucking objects
                bulletController.RunAsyncWithDelay(
                    ExplosionDuration,
                    () =>
                    {
                        bulletController.gameObject.SetActive(false);
                    }
                );
            };

            bulletController.onImpact += (collision, explosion) =>
            {
                if (collision.rigidbody.TryGetComponent(out RayfireRigid rr) && rr.activation.activated) // only delete active objects
                {
                    RayfireMan.DestroyFragment(rr, null);
                }
            };
        }

        /// <summary>
        /// All objects within radius will use physics to be pulled towards the
        /// black hole center. Physics updater stops when attracted object is either
        /// destroyed or disabled.
        /// Do not call on update loop as objects may be double sucked and weird things will happen.
        /// Also performance.
        /// </summary>
        /// <param name="center">Collider that will attract object.</param>
        /// <param name="radius">Everything within this distance from the center will be pulled.</param>
        /// <param name="stopRadius">Everything within this distance will not be pulled.</param>
        /// <param name="forceMagnitude">How strong to pull someone.</param>
        /// <param name="layer">Objects in which layer will be affected.</param>
        /// <param name="buffer">For using overlap sphere. Uses a static buffer if not provided (not recommended).</param>
        public static void SuckEverythingWithinRadius(
            Collider center,
            float radius,
            float stopRadius,
            float forceMagnitude,
            LayerMask layer,
            Collider[] buffer
        )
        {
            var updater = Force.PhysicsUpdate(forceMagnitude, stopRadius);
            Physics.OverlapSphereNonAlloc(center.GetWorldPosition(), radius, buffer, layer);

            foreach (var intersectingCollider in buffer)
            {
                if (intersectingCollider == null) continue;
                
                if (intersectingCollider.gameObject.TryGetComponent(
                    out SimpleReconstructLerp.ReconstructLerpSlave lerpSlave))
                {
                    lerpSlave.StopCurrent();
                }

                // Activate nearby ray fire objects
                if (!intersectingCollider.gameObject.TryGetComponent(out RayfireRigid rayfireRigid))
                {
                    if (!rayfireRigid.initialized) rayfireRigid.Initialize();
                    rayfireRigid.Activate();
                }

                rayfireRigid.physics.rigidBody.useGravity = false;

                // Launch shrinker
                rayfireRigid.ScaleToTarget(new Vector3(0.5f, 0.5f, 0.5f), ExplosionDuration * .8f);
                
                // Launch attractive physics update
                rayfireRigid.RunAsyncFixed(
                    () =>
                    {
                        var centerIsDisabled     = !center.gameObject.activeInHierarchy;
                        var invalidPhysicsUpdate = !updater(intersectingCollider, center.GetWorldPosition());
                        var shouldStop = centerIsDisabled || invalidPhysicsUpdate;

                        if (shouldStop)
                        {
                            rayfireRigid.physics.rigidBody.useGravity = true;
                        }

                        Debug.Log($"Attracting {intersectingCollider.name}");
                        
                        return shouldStop;
                    });
            }
        }
        
        [Button(name: "Attract Everything")]
        public void SuckEverything() => SuckEverythingWithinRadius(
            bulletController.rb.GetComponent<Collider>(),
            suckRadius,
            stopRadius,
            forceSettings.forceMagnitude,
            suckingLayer,
            _attractedObjectBuffer
        );

        private void OnDrawGizmos()
        {
            if (!showPreview) return;

            Gizmos.color = new Color(1f, 0.58f, 0f);
            Gizmos.DrawWireSphere(transform.position, suckRadius);

            for (int i = 0; i < _attractedObjectBuffer.Length; i++)
            {
                var c = _attractedObjectBuffer[i];
                if (c == null) continue;
                
                Gizmos.DrawWireSphere(c.GetWorldPosition(), 0.15f);
            }
            
        }
    }
}