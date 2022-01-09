using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spells.Implementations.Misc
{
    
    /// <summary>
    /// This spell attracts objects within range to the hand of the user while
    /// previewing, and blasts any objects that are within the user's grasp
    /// when executing, dropping on the floor whatever other items
    /// that were not within the grasp.
    /// </summary>
    [CreateAssetMenu(fileName = "Psychokinesis Spell", menuName = "Spells/Psychokinesis", order = 0)]
    public class PsychokinesisSpell : Spell
    {
        [TabGroup("Psychokinesis")]
        public bool useGravity = false;

        [TabGroup("Psychokinesis"), SerializeField] 
        private ForceMode _forceMode = ForceMode.Impulse;
        
        [TabGroup("Psychokinesis")]
        public float castForce = 10f;
        
        [TabGroup("Psychokinesis"), Tooltip("The range of the spell is defined in the targeting settings.")] 
        public Force.Settings forceSettings;

        /// <summary>
        /// Hand preview must know which spell it refers to, so it has access to the settings.
        /// </summary>
        /// <param name="previewEffectInstance"></param>
        public override void PreprocessHandPreviewFX(GameObject previewEffectInstance)
        {
            var component = previewEffectInstance.GetOrElseAddComponent<PsychokinesisSpellPreviewComponent>();
            component.spell = this;
            component.ThisTrigger.radius = range;
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
                
                foreach (var collider in pspc.objectsInRange)
                {
                    if (collider == null) continue;
                    
                    if (collider.TryGetComponent(out Rigidbody rb))
                    {
                        rb.velocity = Vector3.zero;
                        rb.AddForce(direction.normalized * castForce, _forceMode);
                    }
                }
            }
        }
    }
}