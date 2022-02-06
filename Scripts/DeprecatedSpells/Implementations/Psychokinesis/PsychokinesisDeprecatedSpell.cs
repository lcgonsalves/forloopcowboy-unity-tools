using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Misc
{
    
    /// <summary>
    /// This spell attracts objects within range to the hand of the user while
    /// previewing, and blasts any objects that are within the user's grasp
    /// when executing, dropping on the floor whatever other items
    /// that were not within the grasp.
    ///
    /// this spell relies a lot on the prefab,
    /// which needs to have its core collider in a different layer
    /// than the rest of the object such that the preview renders in the
    /// correct layer first person overlay, but that its collisions are
    /// on the Player layer so they avoid being hit by our own projectiles
    /// but also the physics of the core still works in attracting bullets.
    /// </summary>
    [CreateAssetMenu(fileName = "Psychokinesis Spell", menuName = "Spells/Deprecated/Psychokinesis", order = 2)]
    public class PsychokinesisDeprecatedSpell : DeprecatedSpell
    {
        [TabGroup("Psychokinesis")]
        public bool useGravity = false;

        [TabGroup("Psychokinesis"), SerializeField] 
        private ForceMode _forceMode = ForceMode.Impulse;
        
        [TabGroup("Psychokinesis")]
        public float castForce = 10f;
        
        [TabGroup("Psychokinesis"), Tooltip("The range of the spell is defined in the targeting settings.")] 
        public Force.Settings forceSettings;
        

        public override void PreprocessHandPreviewFX(SpellUserBehaviour caster, GameObject previewEffectInstance)
        {
            var component = previewEffectInstance.GetOrElseAddComponent<PsychokinesisSpellPreviewComponent>();
            
            component.spell = this;
            component.trigger.radius = range;
            
            // preview component is aware of who is casting so as to avoid spells cast from the same entity
            component.caster = caster;
        }

        /// <summary>
        /// Returns true if the preview component
        /// has at least 1 object in range to be propelled.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="arm"></param>
        /// <returns></returns>
        public override bool CanCast(SpellUserBehaviour caster, Side<ArmComponent> arm)
        {
            return base.CanCast(caster, arm) &&
                   caster.ParticleInstancesFor(this, arm, out var instances) && 
                   instances.handPreview &&
                   instances.handPreview.TryGetComponent(out PsychokinesisSpellPreviewComponent pspc) &&
                   pspc.objectsInRange.Count > 0;
        }

        protected override void Execute(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {

            if (
                caster.ParticleInstancesFor(this, source, out var instances) && 
                instances.handPreview &&
                instances.handPreview.TryGetComponent(out PsychokinesisSpellPreviewComponent pspc))
            {
                
                Destroy(
                    Instantiate(mainEffect, pspc.PivotPoint, caster.mainCamera.transform.rotation),
                    5f
                );
                
                // immutable list allows for cleanup of objects in range while we iterate the list
                foreach (var collider in pspc.objectsInRange.ToImmutableList())
                {
                    if (collider == null) continue;

                    // call cleanup manually as objects will leave after preview gets disabled.
                    var (rb, bc) = pspc.OnObjectExitCleanup(collider);
                    
                    if (rb)
                    {
                        rb.velocity = Vector3.zero;
                        rb.AddForce(direction.normalized * castForce, _forceMode);
                    }
                }
            }
        }
    }
}