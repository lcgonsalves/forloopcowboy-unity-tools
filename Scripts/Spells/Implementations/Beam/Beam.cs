using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Player;
using UnityEditor;
using UnityEngine;

// Simple beam implementaion
namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Beam
{
    [CreateAssetMenu(fileName = "Beam Spell", menuName = "Spells/Beam", order = 0)]
    public class Beam : Spell
    {
        public Transition sensitivityTransition;

        public float sensitivityDuringBeamCast = 1.0f;

        protected override void Execute(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            Transform start = source.content.GetArmPosition(ArmComponent.ArmPosition.Palm);
            bool particlesAreInstantiated = caster.ParticleInstancesFor(this, source, out var instances);

            // When cast is executed, we BEGIN to draw the beam
            if (particlesAreInstantiated)
            {
                if (instances.main == null) throw new System.NullReferenceException("Beam requires main effect.");

                instances.main.transform.position = start.position;
                instances.main.layer = LayerMask.NameToLayer("Soldiers");
            
                float originalAimSpeed = -1;
                Transition.TransitionState transitionInstance = sensitivityTransition.GetPlayableInstance();

                // control sensitivity according to the uhhhh
                var movementBehaviour = caster.gameObject.GetComponent<AdvancedPlayerMovementBehaviour>();
                if (movementBehaviour != null)
                {
                    // tweenTransition should be like a bell curve if you want things to return to normal == retard
                    originalAimSpeed = movementBehaviour.aimSettings.AimSpeed;

                }


                System.DateTime startTime = System.DateTime.Now;
            
                caster.RunAsyncFixed(
                    () => {
                        instances.main.gameObject.SetActive(true);

                        // not fucking with negative aim speeds man
                        if (movementBehaviour && originalAimSpeed > 0f && movementBehaviour.aimSettings.overloaded != null) {
                            movementBehaviour.aimSettings.overloaded.AimSpeed = Mathf.Lerp(originalAimSpeed, sensitivityDuringBeamCast, transitionInstance.Evaluate(Time.fixedDeltaTime));
                        }

                        // update to track hand
                        instances.main.transform.position = start.position;
                        direction = caster.head.forward;

                        // if within range we cast the beam in the direction from the palm to the target
                        if (Physics.Raycast(caster.head.position, direction, out var hit, range))
                        {
                            instances.main.transform.LookAt(hit.point);
                        }
                        // otherwise we cast the beam from the palm in the given direction (forward from the camera. i.e. not precisely towards the raycast contact point)
                        else
                        {
                            Vector3 targetlessTarget = source.content.transform.position + direction.normalized * range;
                            instances.main.transform.LookAt(targetlessTarget);
                        }

                    },
                    () => DateTime.Now - startTime >= TimeSpan.FromSeconds(sensitivityTransition.duration)
                );

                // now we tell the caster to figure it out and stop drawing the beam in the given duration
                caster.RunAsyncWithDelay(sensitivityTransition.duration, () =>
                {
                    source.content.SetCast(castStyle, false);
                    instances.main.gameObject.SetActive(false);
                });
            }
        }
    }
}
