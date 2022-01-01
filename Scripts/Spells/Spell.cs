using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Player;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelArsenal;
using Object = UnityEngine.Object;

namespace forloopcowboy_unity_tools.Scripts.Spells
{
    /// Defines a skill that can be consumed by the SpellUserBehavior
    public abstract class Spell : SerializedScriptableObject
    {

        [Tooltip("Unique spell identifier.")]
        public string key;

        public float cooldownTimeInSeconds = 0f;

        [Tooltip("How far the spell can reach")]
        public float range = 10f;

        public enum TargetingStyle { Grounded, Ranged }

        public TargetingStyle targetingStyle = TargetingStyle.Ranged;

        public LayerMask raycastLayer;
        
        [Tooltip("When true, spell will preview directly on whatever target transform is set in the spell user behaviour. If false, falls back to whatever targeting style is used.")]
        public bool showPreviewOnCastTarget = false;

        [FormerlySerializedAs("previewEffect")] [Tooltip("The effect that plays in the hand.")]
        public GameObject handPreviewEffect;
        
        [Tooltip("The effect that plays either on the ground, middle of the screen, or on the spell caster's target")]
        public GameObject targetPreviewEffect;

        [Tooltip("The effect that plays in the location where the raycast hits, before the spell is executed.")]
        public GameObject mainEffect;

        [Tooltip("Defines the type of hand animation that plays when holding a spell")]
        public ArmComponent.ChargeStyles chargeStyle = ArmComponent.ChargeStyles.TwoFingerHold;

        [Tooltip("Defines the type of hand animation that plays when casting a spell.")]
        public ArmComponent.CastStyles castStyle = ArmComponent.CastStyles.CastThrow;

        [Tooltip("The time scale that should be used when the characters enter preview mode. If 0 < x < 1 then time is slowed down.")]
        public float slowMoEfect = 1f;

        public float previewScale = 0.23f;
        public float castScale = 0.5f;
        
        public bool debugMode;

        virtual public Vector3 GetTargetPosition([CanBeNull] SpellUserBehaviour caster = null, [CanBeNull] Camera mainCamera = null)
        {
            if (showPreviewOnCastTarget && caster && caster.GetTarget(this, out var target)) return target.transform.position;

            mainCamera = mainCamera ? mainCamera : Camera.main;
            
            // cast a ray forward and if it hits anything, that's the target regardless of the style
            var centerOfScreen = new Vector3(Screen.width / 2f, Screen.height / 2f, mainCamera.nearClipPlane);
            Ray forward = mainCamera.ScreenPointToRay(centerOfScreen);

            if (Physics.Raycast(forward, out var hit, range, raycastLayer) && targetingStyle == TargetingStyle.Ranged)
            {
                return hit.point;
            }
            
            else if (targetingStyle == TargetingStyle.Ranged)
            {
                // project a point range meters away in the direction of the camera
                // to be tested
                return mainCamera.ScreenToWorldPoint(centerOfScreen) + (forward.direction.normalized * range);
            }

            return Vector3.zero;
        }
        
        /// Logic that should run when spell is previewed by spell user
        /// By default, it enables the preview object and sets its position to the source's cast point.
        public virtual void Preview(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {

            if (caster.ParticleInstancesFor(this, source, out var particles))
            {
                var handPreview = particles.handPreview;
                var handPreviewPosition = GetCastPointFor(source);
                var targetPreviewPosition = GetTargetPosition(caster);

                var casterRotation = caster.transform.rotation;
                
                UpdateEffect(handPreview, handPreviewPosition, casterRotation, "FirstPersonObjects");
                UpdateEffect(particles.targetPreview, targetPreviewPosition, casterRotation, "Player", false);
                
            } else NoParticleInstantiatedWarning(caster);
        }

        private void UpdateEffect(GameObject fx, Vector3 position, Quaternion rotation, string layerName, bool applyScale = true)
        {
            var a = fx?.activeInHierarchy;
            var b = fx?.activeSelf;

            if (fx == null) return;
            
            if (!fx.gameObject.activeInHierarchy)
            {
                fx.GetComponentInChildren<VoxelSoundSpawn>()?.Start();
                fx.gameObject.SetActive(true);
            }

            var fxTr = fx.transform;
            
            fxTr.position = position;
            fxTr.rotation = rotation;
            
            var fxLayer = LayerMask.NameToLayer(layerName);
            if (fx.layer != fxLayer)
                fx.SetLayerRecursively(fxLayer);
            
            if (applyScale && fxTr.childCount > 0)
                fxTr.GetChild(0).localScale = previewScale * Vector3.one;
        }

        /// Stops any ongoing preview
        /// By default, it disables the preview object.
        public virtual void ResetPreview(SpellUserBehaviour caster, Side<ArmComponent> source)
        {
            if (caster.ParticleInstancesFor(this, source, out var particles))
            {
                if (particles.handPreview) particles.handPreview.gameObject.SetActive(false);
                if (particles.targetPreview) particles.targetPreview.gameObject.SetActive(false);
            }

            else NoParticleInstantiatedWarning(caster); 
        }

        /// Logic that should run when spell is executed by spell user
        protected abstract void Execute(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction);

        /// Casts the spell from proper arm position
        public bool Cast(SpellUserBehaviour caster, Side<ArmComponent> source, Vector3 direction)
        {
            bool canCast = CanCast(caster, source, out var time);
            if (canCast) Execute(caster, source, direction);
            return canCast;
        }

        /// See CanCast(SpellUserBehaviour caster,  out System.DateTime time)
        public bool CanCast(SpellCaster caster, Side<ArmComponent> arm)
        {
            return CanCast(caster, arm, out var _);
        }

        public bool CanCast(SpellCaster caster, Side<ArmComponent> arm, out System.DateTime time)
        {
            bool canHold = CanHold(caster, arm, out time);

            return arm.content.holdReady && canHold;
        }

        public bool CanHold(SpellCaster caster, Side<ArmComponent> arm)
        {
            return CanHold(caster, arm, out var _);
        }

        // true if a spell has never been casted before or if the cooldown is over
        public bool CanHold(SpellCaster caster, Side<ArmComponent> arm, out System.DateTime time)
        {
            bool spellNeverCastedBefore = caster.LatestSpellCastTimeFor(this, arm, out time) && time == DateTime.MinValue;
            bool spellHasBeenCastedBefore = !spellNeverCastedBefore;

            // here we define: if a spell has never been casted before (for whatever reason we couldn't get the latest cast time) we just let the user cast it
            return (spellHasBeenCastedBefore && ((System.DateTime.Now - time).TotalSeconds > cooldownTimeInSeconds)) ||
                   spellNeverCastedBefore;
        }

        public interface InstanceConfiguration
        {
            GameObject handPreview { get; }
            
            GameObject targetPreview { get; }
            
            GameObject main { get; }
        }

        public interface SpellCaster
        {
            // Getter for fetching instantiated emitters for the given spell
            bool ParticleInstancesFor(Spell spell, Side<ArmComponent> arm, out InstanceConfiguration instances);

            // Getter for fetching latest cast time to calculate cooldown
            bool LatestSpellCastTimeFor(Spell spell, Side<ArmComponent> arm, out System.DateTime time);
        }

        protected static void CreateSpell<S>(string name) where S : Spell
        {
            Object s = ScriptableObject.CreateInstance<S>();
            AssetDatabase.CreateAsset(s, $"Assets/Spells/New{name}.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();

            Selection.activeObject = s;
        }

        protected Vector3 GetCastPointFor(Side<ArmComponent> source)
        {
            return source.content.GetCastPoint(chargeStyle);
        }

        private void NoParticleInstantiatedWarning(Object caster)
        {
            if (debugMode)
                Debug.LogWarning($"Caster {caster.name} has no particles for {this.name}. Make sure particles are initialized properly. Particles are optional, but their preparation in the Caster is required.");
        }

    }
}
