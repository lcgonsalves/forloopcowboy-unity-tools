using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.BehaviorDesignerTasks;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Player;
using forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace forloopcowboy_unity_tools.Scripts.Spells
{
    public class SpellUserBehaviour : MonoBehaviour, Spell.SpellCaster
    {

        [UnityEngine.Tooltip("Allowed Spells")]
        public List<Spell> spells;
        
        public bool GetTarget(Spell spell, out GameObject closestTarget)
        {
            closestTarget = null;
            gm = gm == null ? FindObjectOfType<GameplayManager>() : gm;
            playerComponent = playerComponent == null ? GetComponent<PlayerComponent>() : playerComponent;

            if (playerComponent == null) return false;
            return gm.FindClosestTarget(this.transform.position, spell.range, playerComponent.side.GetOpposing(), out closestTarget);
        }

        public GameplayManager gm = null;
        private PlayerComponent playerComponent;
        
        public InputActionReference leftHandInput;
        public InputActionReference rightHandInput;

        public InputActionReference actionSelect;

        [FormerlySerializedAs("holdToSelectOtherAction")] public InputActionReference holdToSelectLeftAction;

        public Chronos.Chronos chronos { get; private set; }


        /// State management

        [SerializeField, ReadOnly]
        private Core.Tuple<Spell, Spell> activeSpell;

        [SerializeField, ReadOnly]
        private bool previewingLeftHandSpell = false;

        [SerializeField, ReadOnly]
        private bool previewingRightHandSpell = false;

        private class Config : Spell.InstanceConfiguration
        {
            // cannot be instantiated from outside of this context
            internal Config() { }

            public GameObject handPreview = null;
            public GameObject main = null;
            public GameObject targetPreview = null;

            GameObject Spell.InstanceConfiguration.handPreview => handPreview;
            GameObject Spell.InstanceConfiguration.main => main;

            GameObject Spell.InstanceConfiguration.targetPreview => targetPreview;
        }


        // fixme: fuck
        // update: still not fixing this
        private Core.Tuple<Dictionary<Spell, Spell.InstanceConfiguration>, Dictionary<Spell, Spell.InstanceConfiguration>> spellParticles = 
            new Core.Tuple<Dictionary<Spell, Spell.InstanceConfiguration>, Dictionary<Spell, Spell.InstanceConfiguration>>(
                new Dictionary<Spell, Spell.InstanceConfiguration>(), new Dictionary<Spell, Spell.InstanceConfiguration>()
            );

        private Core.Tuple<Dictionary<Spell, System.DateTime>, Dictionary<Spell, System.DateTime>> latestSpellCastTime =
            new Core.Tuple<Dictionary<Spell, System.DateTime>, Dictionary<Spell, System.DateTime>>(
                new Dictionary<Spell, System.DateTime>(), new Dictionary<Spell, System.DateTime>()
            );

        public bool ParticleInstancesFor(Spell spell, Side<ArmComponent> arm, out Spell.InstanceConfiguration instances)
        {
            // spell particles are shared between both hands
            return spellParticles.Get(arm).TryGetValue(spell, out instances);
        }

        public bool LatestSpellCastTimeFor(Spell spell, Side<ArmComponent> arm, out System.DateTime time)
        {
            return latestSpellCastTime.Get(arm).TryGetValue(spell, out time);
        }

        [SerializeField, ReadOnly]
        public Camera mainCamera = null;

        [SerializeField, ReadOnly]

        private RaycastHit spellTargetRaycastHit;

        [SerializeField, ReadOnly]

        private Vector3 spellTargetPosition = Vector3.zero;

        [SerializeField, ReadOnly]

        private bool targetWithinRange = false;

        public Core.Tuple<ArmComponent, ArmComponent> arms = new Core.Tuple<ArmComponent, ArmComponent>();

        public ArmComponent leftArm { get { return arms.Left; } }
        public ArmComponent rightArm { get { return arms.Right; } }

        public Transform head;

        private int selectedSpellIndex = 0;

        /// Casts active spell with left hand
        public void CastSpellWithLeftHand()
        {
            if (activeSpell.Left != null)
            {
                CastSpell(arms.l);
            }
        }

        /// Casts active spell with right hand
        public void CastSpellWithRightHand()
        {
            if (activeSpell.Right != null)
            {
                CastSpell(arms.r);
            }
        }

        private void Update()
        {
            ArmComponent.ChargeStyles activeSpellChargeStyleL = activeSpell.Get(arms.l).chargeStyle;
            ArmComponent.ChargeStyles activeSpellChargeStyleR = activeSpell.Get(arms.r).chargeStyle;

            previewingRightHandSpell = rightArm.IsHoldingAndReady(activeSpellChargeStyleR);
            previewingLeftHandSpell  = leftArm.IsHoldingAndReady(activeSpellChargeStyleL);
        }

        private void FixedUpdate()
        {
            if (previewingLeftHandSpell) PreviewSpell(arms.l);
            else activeSpell?.Left?.ResetPreview(this, arms.l);

            if (previewingRightHandSpell) PreviewSpell(arms.r);
            else activeSpell?.Right?.ResetPreview(this, arms.r);
        }

        private void OnEnable()
        {

            // fetch or iniginialize the chronos instance
            chronos = this.GetOrElseAddComponent<Chronos.Chronos>();

            leftHandInput.action.Enable();
            rightHandInput.action.Enable();
            actionSelect.action.Enable();
            holdToSelectLeftAction.action.Enable();
            // set up updater
            actionSelect.action.performed += ctx =>
            {
                Side<ArmComponent> selectedArm;

                if (holdToSelectLeftAction.action.ReadValueAsObject() != null)
                {
                    selectedArm = arms.l;
                }
                else selectedArm = arms.r;

                Spell aspell = activeSpell.Get(selectedArm);
                ArmComponent.ChargeStyles cs = aspell.chargeStyle;
                bool wasHolding = selectedArm.content.IsHolding(cs);
                aspell.ResetPreview(this, selectedArm);

                // If selecting with given arm, reset hold position.
                if (wasHolding) {
                    selectedArm.content.DisableHold();
                }

                var direction = Mathf.RoundToInt(Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f));

                selectedSpellIndex += direction;
                selectedSpellIndex = selectedSpellIndex < 0 ? spells.Count - 1 : selectedSpellIndex;
                selectedSpellIndex = selectedSpellIndex >= spells.Count ? 0 : selectedSpellIndex;
                selectedSpellIndex = Mathf.Clamp(selectedSpellIndex, 0, spells.Count - 1);

                if (selectedArm is Left<ArmComponent>)
                {
                    activeSpell.l = new Left<Spell>(spells[selectedSpellIndex]);
                }
                else
                {
                    activeSpell.r = new Right<Spell>(spells[selectedSpellIndex]);
                }

                // automatically begin holding the selected spell
                if (wasHolding) this.RunAsyncWithDelay(0.08f, () => {
                    Spell newSpell = activeSpell.Get(selectedArm);
                    Debug.Log("setting " + newSpell.chargeStyle.ToString() + "to true for spell " + newSpell.name);
                    selectedArm.content.SetHolder(newSpell.chargeStyle, true);
                });

            };

            // press down - activate preview if can cast
            leftHandInput.action.started += ctx => {
                Spell s = activeSpell.Get(arms.l);

                if (s.CanHold(this, arms.l)) arms.Left.SetHolder(s.chargeStyle, true);
            };
            rightHandInput.action.started += ctx => { 
                Spell s = activeSpell.Get(arms.r);

                if (s.CanHold(this, arms.r)) arms.Right.SetHolder(s.chargeStyle, true);
            };

            // lift up - begin cast animation that will trigger spell
            leftHandInput.action.canceled += ctx =>
            {
                Spell spell = activeSpell.Get(arms.l);
                if (leftArm && spell.CanCast(this, arms.l)) leftArm.InitiateCastIfHolding(spell.castStyle, spell.chargeStyle);
                else leftArm.DisableHold();
                chronos.Normalize();
            };
            rightHandInput.action.canceled += ctx =>
            {
                Spell spell = activeSpell.Get(arms.r);
                if (rightArm && spell.CanCast(this, arms.r)) rightArm.InitiateCastIfHolding(spell.castStyle, spell.chargeStyle);
                else rightArm.DisableHold();
                chronos.Normalize();
            };

            // handle cast animation
            if (leftArm) leftArm.OnCast += () => CastSpellWithLeftHand();
            if (rightArm) rightArm.OnCast += () => CastSpellWithRightHand();

            // initialize active spell
            activeSpell.l = spells.Count > 0 ? new Left<Spell>(spells[selectedSpellIndex]) : null;
            activeSpell.r = spells.Count > 0 ? new Right<Spell>(spells[selectedSpellIndex]) : null;

            foreach (var spell in spells)
            {
                // perhaps in the futue we can have different spell selections for each arm, but for now both arms are capable by default

                spellParticles.Left.Add(spell, InstantiateParticles(spell)); // init instances
                spellParticles.Right.Add(spell, InstantiateParticles(spell)); // init instances

                latestSpellCastTime.Left.Add(spell, System.DateTime.MinValue); // init date
                latestSpellCastTime.Right.Add(spell, System.DateTime.MinValue); // init date
            }

            // get camera for raycasting
            mainCamera = Camera.main;

            // Warning checks
            if (!activeSpell.Left || !activeSpell.Right) Debug.LogWarning("No spells defined in SpellUser behavior.");
            if (!mainCamera) Debug.LogWarning("No main camera in scene. Spell User Behavior requires camera for raycasting.");

        }

        private Config InstantiateParticles(Spell spell)
        {
            var spellParticleInstanceContainer = new Config();
            
            GameObject InitializeEffect(string key, GameObject fx)
            {
                GameObject previewInstance = GameObject.Instantiate(fx, transform.position, transform.rotation);

                previewInstance.name = $"{key} {fx.name}";
                previewInstance.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FirstPersonObjects"));
                previewInstance.gameObject.SetActive(false);

                return previewInstance;
            }

            if (spell.handPreviewEffect)
            {
                spellParticleInstanceContainer.handPreview = InitializeEffect("Hand Preview", spell.handPreviewEffect);
                spell.PreprocessHandPreviewFX(spellParticleInstanceContainer.handPreview);
            }
            else Debug.LogWarning("No HAND preview particle assigned in spell definition. Please update the asset.");

            if (spell.targetPreviewEffect)
            {
                spellParticleInstanceContainer.targetPreview = InitializeEffect("Target Preview", spell.targetPreviewEffect);
                spell.PreprocessTargetPreviewFX(spellParticleInstanceContainer.targetPreview);
            }
            else Debug.LogWarning("No TARGET preview particle assigned in spell definition. Please update the asset.");

            if (spell.mainEffect)
            {
                spellParticleInstanceContainer.main = InitializeEffect("Main", spell.mainEffect);
                spell.PreprocessMainFX(spellParticleInstanceContainer.main);
            }
            else Debug.LogWarning("No MAIN particle assigned in spell definition. Please update the asset.");
            
            return spellParticleInstanceContainer;
        }

        private bool CastSpell(Side<ArmComponent> arm)
        {
            bool previewingCorrectHand =
                (arm is Left<ArmComponent> && previewingLeftHandSpell) ||
                (arm is Right<ArmComponent> && previewingRightHandSpell);

            if (previewingCorrectHand)
            {
                Spell active = activeSpell.Get(arm);

                var justCasted = active.Cast(this, arm, mainCamera.transform.TransformDirection(Vector3.forward));
                if (justCasted) latestSpellCastTime.Get(arm)[activeSpell.Get(arm)] = System.DateTime.Now;

                // if just casted, stop previewing. if didn't cast (and by the previous 'if', also previewing), continue to preview
                if (arm is Left<ArmComponent>) previewingLeftHandSpell = false;
                if (arm is Right<ArmComponent>) previewingRightHandSpell = false;

                return justCasted;

            }

            return false;
        }

        private void PreviewSpell(Side<ArmComponent> arm)
        {
            activeSpell.Get(arm).Preview(this, arm, mainCamera.transform.TransformDirection(Vector3.forward));
        }

        private void OnDestroy()
        {
            foreach (var spell in spells)
            {
                DestroyInstances(spell, arms.l);
                DestroyInstances(spell, arms.r);
            }

            leftHandInput.action.Disable();
            rightHandInput.action.Disable();
            actionSelect.action.Disable();
            holdToSelectLeftAction.action.Disable();

            void DestroyInstances(Spell spell, Side<ArmComponent> side)
            {
                Spell.InstanceConfiguration instances = null;

                if (spellParticles.Get(side)?.TryGetValue(spell, out instances) ?? false)
                {
#if UNITY_EDITOR
                    if (instances.main != null) GameObject.DestroyImmediate(instances.main);
                    if (instances.handPreview != null) GameObject.DestroyImmediate(instances.handPreview);
#endif

                    if (instances.main != null) GameObject.Destroy(instances.main);
                    if (instances.handPreview != null) GameObject.Destroy(instances.handPreview);

                }
            }
        } 

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            if (leftArm) Gizmos.DrawWireSphere(leftArm.transform.position, 0.1f);
            if (rightArm) Gizmos.DrawWireSphere(rightArm.transform.position, 0.1f);
        }
        public bool CanCast<T>(Side<T> side)
        {
            return activeSpell.Get(side).CanCast(this, side);
        }

    }
}
