using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using forloopcowboy_unity_tools.Scripts.Weapon;
using Sirenix.OdinInspector;
using UnityEditor.Animations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.GameLogic
{
    /// <summary>
    /// Picks from a set of character prefabs, a set of weapons
    /// and exposes functions for instantiating variations.
    /// </summary>
    [CreateAssetMenu(fileName = "Untitled Soldier Randomizer Settings", menuName = "Settings/NPC/New NPC Randomizer Settings...", order = 2)]
    public class SoldierRandomizer : SerializedScriptableObject
    {
        public uint prefabRandomizerSeed = 69;
        
        public Transition aimTransition;
        public AnimatorController animatorController;
        public ExternalBehaviorTree ikControlBehaviorTree;
        public ExternalBehaviorTree combatBehaviorTree;
        public Transition ikLerpInTransition;
        public Transition ikLerpOutTransition;

        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public BulletImpactSettings bulletImpactSettings;
        
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public StringList firstNames;
        
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public StringList lastNames;

        public NPCSettings npcSettings;

        [ShowInInspector]
        public static string soldierTag = "Soldier";
        
        // possible models to spawn
        public List<NPCPropComponent> randomizableCharacters;
        public List<GameObject> presetCharacters;
        public List<Weapon.Weapon> weapons;
        public int maxNumberOfWeapons = 2;

        /// <summary>
        /// To prevent the same rig prefab from being picked
        /// repeatedly, we maintain a stack of indices that is
        /// re-filled when it is empty, by randomly picking indices.
        /// </summary>
        protected Stack<GameObject> prefabsToInstantiate = new Stack<GameObject>();

        private void OnEnable()
        {
            Random.InitState((int) prefabRandomizerSeed);
            
            RefreshPrefabStack();
        }

        private void RefreshPrefabStack()
        {
            RefreshPrefabStack(prefabRandomizerSeed);
        }

        private void ReRandomizePrefabStack()
        {
            RefreshPrefabStack( (uint) Random.Range(0, int.MaxValue));
        }
        
        private void RefreshPrefabStack(uint newSeed)
        {
            var random = new Unity.Mathematics.Random(newSeed);
            var randomized = presetCharacters.OrderBy((o => random.NextInt()));
            prefabsToInstantiate = new Stack<GameObject>(randomized);
        }

        [Button] public Soldier InstantiateAndReparentAndMaybeRandomize(Transform positionAnchor, bool reparent = false, GameplayManager manager = null)
        {
            bool shouldRandomize = Random.Range(0f, 1f) > 0.005f;
            
            var soldier = this.Instantiate(positionAnchor.position, shouldRandomize);
            if (reparent)
            {
                soldier.transform.SetParent(positionAnchor);
                soldier.transform.localRotation = Quaternion.identity;
            }
            return soldier;
        }
        
        /// <summary>
        /// Picks a random charactar and spawns it.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="randomize">When true, also randomizes character's props.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Soldier Instantiate(Vector3 position, bool randomize = false, GameplayManager manager = null)
        {
            if (randomizableCharacters.Count == 0) throw new Exception("No soldier prefabs specified. Please add a prefab.");
            if (weapons.Count == 0) throw new Exception("No weapon definitions specified. Please add at least one.");

            // make sure we don't run out of randomized prefabs
            if (prefabsToInstantiate.Count == 0) ReRandomizePrefabStack();
            
            GameObject selectedCharacter;
            if (randomize)
            {
                var propComponent = randomizableCharacters[Random.Range(0, randomizableCharacters.Count)];
                selectedCharacter = propComponent.gameObject;
            }
            else
                selectedCharacter = prefabsToInstantiate.Pop();

            var numIndices = Random.Range(1, maxNumberOfWeapons);
            var indices = new HashSet<int>();
            while (indices.Count < numIndices || indices.Count < weapons.Count)
            {
                indices.Add(Random.Range(0, weapons.Count));
            }

            var selectedWeapons = indices.Select(idx => weapons[idx]).ToArray();

            var soldier = InstantiateCharacter(
                selectedCharacter,
                animatorController,
                ikControlBehaviorTree,
                combatBehaviorTree,
                bulletImpactSettings,
                aimTransition,
                ikLerpInTransition,
                ikLerpOutTransition,
                firstNames,
                lastNames,
                npcSettings,
                manager
            )(selectedWeapons, position);

            if (!string.IsNullOrEmpty(soldierTag)) soldier.gameObject.tag = soldierTag;

            return soldier;
        }

        /// <summary>
        /// Returns a function that generates the character prefab, given a set of weapons.
        /// </summary>
        /// <param name="characterRigPrefab">Character with rig</param>
        /// <param name="animatorController">Animator controller with animations compatible with the other components.</param>
        /// <param name="ikControlBehaviorTree">Behaviour tree for controlling IK</param>
        /// <param name="aimTransition">Transition lerp for aiming</param>
        /// <param name="ikLerpInTransition"></param>
        /// <param name="ikLerpOutTransition"></param>
        /// <param name="firstNames">List of potential first names to be randomized</param>
        /// <param name="lastNames">List of potential last names to be randomized</param>
        /// <returns></returns>
        public static Func<IEnumerable<Weapon.Weapon>, Vector3, Soldier> InstantiateCharacter(
            GameObject characterRigPrefab,
            AnimatorController animatorController,
            ExternalBehaviorTree ikControlBehaviorTree,
            ExternalBehaviorTree combatBehaviorTree,
            BulletImpactSettings bulletImpactSettings,
            Transition aimTransition,
            Transition ikLerpInTransition,
            Transition ikLerpOutTransition,
            StringList firstNames,
            StringList lastNames,
            NPCSettings npcSettings,
            GameplayManager gameplayManager
        )
        {
            return (weapons, position) =>
            {
                var instance = Instantiate(characterRigPrefab, position, Quaternion.identity);
                
                // initialize health
                var health = instance.GetOrElseAddComponent<HealthComponent>();

                // initialize ragdoll component (or remove if prefab doesn't have any rigidbodies)
                var ragdoll = instance.GetOrElseAddComponent<Ragdoll>();
                ragdoll.AttachImpactParticleSpawners(bulletImpactSettings);
                ragdoll.InitializeKeyLimbCache();

                // initialize navigation
                var navigation = instance.GetOrElseAddComponent<AdvancedNavigation>();
                navigation.animatorUpdateSettings = new AdvancedNavigation.AnimatorUpdateSettings
                {
                    enabled = true,
                    updateVelocity = true
                };

                // initialize aim component
                var aimComponentWithIK = instance.GetOrElseAddComponent<AimComponentWithIK>();
                var animator = instance.GetComponent<Animator>();
                animator.applyRootMotion = false;
                
                aimComponentWithIK.easeToAimTransition = aimTransition;
                aimComponentWithIK.ikLerpIn = ikLerpInTransition;
                aimComponentWithIK.ikLerpOut = ikLerpOutTransition;

                animator.runtimeAnimatorController = animatorController;
                
                // initialize character stats
                var stats = instance.GetOrElseAddComponent<NPCAttributeComponent>();
                stats.possibleFirstNames = firstNames;
                stats.possibleLastNames = lastNames;

                stats.Randomize();
                
                // set health according to armor rating
                var armorRating = npcSettings.armorSettingsPerNumberOfStars.GetRatingFor(stats.armor);
                health.SetMaxHealth(health.MaxHealth + armorRating);

                // randomize props
                var propRandomizer = instance.GetComponent<NPCPropComponent>();
                if (propRandomizer)
                {
                    // note you may have a prop randomizer on the character and don't want to randomize its props
                    // that is allowed by setting the randomizable property to false
                    propRandomizer.RandomizeAll();
                }

                // initialize weapon user compoonent
                var weaponUserComponent = instance.GetOrElseAddComponent<WeaponUser>();
                var triggerHandTransform =
                    instance.transform.Find(
                        "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R");
                weaponUserComponent.triggerHandTransform = triggerHandTransform;
                weaponUserComponent.reloadHandTransform = ragdoll.handL.Get;
                weaponUserComponent.animatorSettings = new WeaponUser.AnimatorIntegrationSettings(true);

                // Set accuracy processor to processor defined for the given number of stars
                if (npcSettings.accuracySettingsPerNumberOfStars.ContainsKey(stats.accuracy))
                    weaponUserComponent.accuracyProcessor =
                        npcSettings.accuracySettingsPerNumberOfStars[stats.accuracy];
                
                // initialize animation parameters
                foreach (var weaponType in EnumUtil.GetValues<WeaponUser.WeaponType>())
                {
                    switch (weaponType)
                    {
                        case WeaponUser.WeaponType.Primary:
                            weaponUserComponent.animatorSettings.animatorParameters.Add(
                                new WeaponUser.AnimatorIntegrationSettings.WeaponTypeAnimParams(weaponType, "UsingRifle")
                            );
                            break;
                        case WeaponUser.WeaponType.Secondary:
                            weaponUserComponent.animatorSettings.animatorParameters.Add(
                                new WeaponUser.AnimatorIntegrationSettings.WeaponTypeAnimParams(weaponType, "UsingPistol")
                            );
                            break;
                        default:
                            weaponUserComponent.animatorSettings.animatorParameters.Add(
                                new WeaponUser.AnimatorIntegrationSettings.WeaponTypeAnimParams(weaponType));
                            break;
                    }
                }

                var potentialHolsters = new List<IKHolster>(instance.transform.GetComponentsInChildren<IKHolster>());
                
                // for fresh objects we define a primary and secondary holsters
                if (potentialHolsters.Count == 0)
                {
                    var backHolster = new GameObject("BackHolster");
                    backHolster.transform.SetParent(instance.transform.Find("Root"));
                    backHolster.transform.localPosition = new Vector3(-0.072f, 1.386f, -0.2006302f); // from editor fiddling
                    backHolster.transform.localRotation = Quaternion.Euler(75, 90, 0);

                    var bhComponent = backHolster.AddComponent<IKHolster>();
                    bhComponent.type = WeaponUser.WeaponType.Primary;
                    
                    potentialHolsters.Add(bhComponent);
                    
                    var sideHolster = new GameObject("SideHolster");
                    sideHolster.transform.SetParent(instance.transform.Find("Root"));
                    sideHolster.transform.localPosition = new Vector3(0.138999999f,0.842999995f,-0.181999996f); // from editor fiddling
                    sideHolster.transform.localRotation = Quaternion.Euler(75, 0, -43.592f);
                    
                    var shComponent = sideHolster.AddComponent<IKHolster>();
                    shComponent.type = WeaponUser.WeaponType.Secondary;
                    
                    potentialHolsters.Add(shComponent);
                }

                // instantiate weapons
                foreach (Weapon.Weapon weapon in weapons)
                {
                    var weaponInstance = Instantiate(weapon.prefab, triggerHandTransform);
                    var type = weapon.inventorySettings.type;

                    var controller = weaponInstance.GetComponent<WeaponController>();
                    controller.weaponSettings = weapon;
                    var item = WeaponUser.GetCorrectiveTransformsFromAsset(new WeaponUser.WeaponItem(controller, type));

                    WeaponUser.ApplyTransformationsToWeapon(item); // in case the thing has no holster
                    
                    var potentialHolster = potentialHolsters.Find(_ => _.type == type);
                    
                    if (potentialHolster != null)
                    {
                        
                        var holster = new WeaponUser.WeaponHolster(potentialHolster.transform, type, item);
                        WeaponUser.ApplyTransformationsToWeapon(holster);
                        weaponUserComponent.holsters.Add(holster);
                        potentialHolsters.Remove(potentialHolster);
                        
                        var ikSettings = new WeaponIKSettings();
                    
                        ikSettings.limb = weapon.ikSettings.limb;

                        ikSettings.translation.value = weapon.ikSettings?.translation?.value ?? Vector3.zero;
                        ikSettings.translation.weight = weapon.ikSettings?.translation?.weight ?? 0f;

                        ikSettings.rotation.value = weapon.ikSettings?.rotation?.value ?? Vector3.zero;
                        ikSettings.rotation.weight = weapon.ikSettings?.rotation?.weight ?? 0f;

                        ikSettings.target = weaponInstance.transform.FindRecursively(_ => _.name == weapon.ikSettings.path);

                        ikSettings.forWeapon = controller;
                        weaponUserComponent.inventory.Add(item);
                        aimComponentWithIK.supportHandIKSettings.Add(ikSettings);
                        
                    }
                    else
                    {
                        // Debug.Log($"No potential holster found for weapon {weapon.name} of type {type}. This weapon will not be attached.");
                        DestroyImmediate(weaponInstance);
                    }
                    
                }

                aimComponentWithIK.Initialize();

                // initialize ik controller
                var ikBehaviourTree = instance.AddComponent<BehaviorTree>();
                ikBehaviourTree.ExternalBehavior = ikControlBehaviorTree;
                ikBehaviourTree.BehaviorName = "IKController";
                ikBehaviourTree.SetVariableValue("Self", instance.gameObject);

                var combatBehavior = instance.AddComponent<BehaviorTree>();
                combatBehavior.ExternalBehavior = combatBehaviorTree;
                combatBehavior.BehaviorName = "CombatController";
                combatBehavior.SetVariableValue("Self", instance.gameObject);
                combatBehavior.SetVariableValue("GameplayManager", gameplayManager.gameObject);
                
                // Change name
                instance.name = stats.FullName;
                
                return new Soldier()
                {
                    gameObject = instance,
                    transform = instance.transform,
                    aimComponentWithIK = aimComponentWithIK,
                    animator = animator,
                    navigation = navigation,
                    weaponUserComponent = weaponUserComponent,
                    attributes = stats,
                    health = health,
                    ragdoll = ragdoll
                };
            };
        }

    }
    
    [Serializable]
    public struct Soldier
    {
        public GameObject gameObject;
        public Transform transform;
        public AdvancedNavigation navigation;
        public AimComponentWithIK aimComponentWithIK;
        public Animator animator;
        public WeaponUser weaponUserComponent;
        public NPCAttributeComponent attributes;
        public HealthComponent health;
        public Ragdoll ragdoll;
        
        // helpers
        public void EquipActiveWeapon()
        {
            weaponUserComponent.EquipWeapon(weaponUserComponent.Active);
        }
    }

}