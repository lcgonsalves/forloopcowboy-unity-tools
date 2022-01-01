using System;
using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEditor.UIElements;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile
{
    public class BlackHoleSuctionComponent : SerializedMonoBehaviour
    {
        public bool showPreview = true;
        
        public bool suckOnSpawnEnabled = true;
        public float suckOnSpawnDelay = .8f;
        public LayerMask suckingLayer;
        
        public float suckRadius = 1f;

        [InlineEditor(InlineEditorModes.FullEditor)]
        public Transition suctionTransition;
        
        [InlineEditor(InlineEditorModes.FullEditor)]
        public Transition scaleDownTransition;
        
        public List<GameObject> allWhomHaveBeenSucked = new List<GameObject>();

        private void Start()
        {
            if (suckOnSpawnEnabled)
            {
                this.RunAsyncWithDelay(
                    suckOnSpawnDelay,
                    () => allWhomHaveBeenSucked.AddRange(SuckEverythingWithinRadius())
                );
            }
        }

        /// <summary>
        /// All objects within radius shring to a small size and get lerped
        /// towards the centre of the black hole.
        /// </summary>
        /// <returns>All objects that were sucked and disabled.</returns>
        public GameObject[] SuckEverythingWithinRadius()
        {
            var intersectingColliders = Physics.OverlapSphere(transform.position, suckRadius, suckingLayer);

            foreach (var intersectingCollider in intersectingColliders)
            {
                if (intersectingCollider.gameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }

                if (intersectingCollider.gameObject.TryGetComponent(
                    out SimpleReconstructLerp.ReconstructLerpSlave lerpSlave))
                {
                    lerpSlave.StopCurrent();
                }

                var targetTransform = intersectingCollider.transform;

                var startingScale = targetTransform.localScale;
                float scaleWhenTiny = 0.1f;
                
                suctionTransition.LerpTransform(
                    this,
                    targetTransform,
                    targetTransform,
                    transform,
                    suctionTransition.duration,
                    () =>
                    {
                        targetTransform.gameObject.SetActive(false);
                    }
                );
                
                StartCoroutine(
                    scaleDownTransition.PlayOnceWithUpdater(
                        state =>
                        {
                            if (!targetTransform.gameObject.activeInHierarchy) return;
                            
                            var interpolatedScale =
                                Vector3.Lerp(startingScale, startingScale * scaleWhenTiny, state.Snapshot());

                            targetTransform.localScale = interpolatedScale;
                        },
                        _ => { targetTransform.localScale = startingScale * scaleWhenTiny; }
                    )    
                );
            }

            return intersectingColliders.Select(_ => _.gameObject).ToArray();
        }


        private void OnDrawGizmos()
        {
            if (!showPreview) return;

            Gizmos.color = new Color(1f, 0.58f, 0f);
            Gizmos.DrawWireSphere(transform.position, suckRadius);
        }
    }
}