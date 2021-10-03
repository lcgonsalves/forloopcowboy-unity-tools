using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
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
    [CreateAssetMenu(fileName = "Untitled Soldier Randomizer Settings", menuName = "NPC/New Soldier Randomizer Settings...", order = 2)]
    public class SoldierRandomizer : ScriptableObject
    {
        public uint prefabRandomizerSeed = 69;
        
        public Transition aimTransition;
        public AnimatorController animatorController;
        public ExternalBehaviorTree ikControlBehaviorTree;
        public Transition ikLerpInTransition;
        public Transition ikLerpOutTransition;
        public StringList firstNames;
        public StringList lastNames;
        
        // possible models to spawn
        public List<GameObject> characterRigPrefabs;
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
            var randomized = characterRigPrefabs.OrderBy((o => random.NextInt()));
            prefabsToInstantiate = new Stack<GameObject>(randomized);
        }

        [Button] public Soldier Instantiate(Transform positionAnchor, bool reparent = false)
        {
            var soldier = this.Instantiate(positionAnchor.position);
            if (reparent)
            {
                soldier.transform.SetParent(positionAnchor);
                soldier.transform.localRotation = Quaternion.identity;
            }
            return soldier;
        }
        
        public Soldier Instantiate(Vector3 position)
        {
            if (characterRigPrefabs.Count == 0) throw new Exception("No soldier prefabs specified. Please add a prefab.");
            if (weapons.Count == 0) throw new Exception("No weapon definitions specified. Please add at least one.");

            // make sure we don't run out of randomized prefabs
            if (prefabsToInstantiate.Count == 0) ReRandomizePrefabStack();
            
            var selectedCharacter = prefabsToInstantiate.Pop();

            var numIndices = Random.Range(1, maxNumberOfWeapons);
            var indices = new HashSet<int>();
            while (indices.Count < numIndices || indices.Count < weapons.Count)
            {
                indices.Add(Random.Range(0, weapons.Count));
            }

            var selectedWeapons = indices.Select(idx => weapons[idx]).ToArray();

            return InstantiateCharacter(
                selectedCharacter,
                animatorController,
                ikControlBehaviorTree,
                aimTransition,
                ikLerpInTransition,
                ikLerpOutTransition,
                firstNames,
                lastNames
            )(selectedWeapons, position);
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
            Transition aimTransition,
            Transition ikLerpInTransition,
            Transition ikLerpOutTransition,
            StringList firstNames,
            StringList lastNames
        )
        {
            return (weapons, position) =>
            {
                var instance = Instantiate(characterRigPrefab, position, Quaternion.identity);

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
                aimComponentWithIK.easeToAimTransition = aimTransition;
                aimComponentWithIK.ikLerpIn = ikLerpInTransition;
                aimComponentWithIK.ikLerpOut = ikLerpOutTransition;

                animator.runtimeAnimatorController = animatorController;
                
                // initialize character stats
                var stats = instance.GetOrElseAddComponent<NPCAttributeComponent>();
                stats.possibleFirstNames = firstNames;
                stats.possibleLastNames = lastNames;
                
                stats.Randomize();

                // initialize weapon user compoonent
                var weaponUserComponent = instance.GetOrElseAddComponent<WeaponUser>();
                var triggerHandTransform =
                    instance.transform.Find(
                        "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R");
                weaponUserComponent.triggerHandTransform = triggerHandTransform;
                weaponUserComponent.animatorSettings = new WeaponUser.AnimatorIntegrationSettings(true);

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

                var potentialHolsters = new List<Transform>();
                
                instance.transform.FindAllRecursively(_ => _.CompareTag("IKHolster"), potentialHolsters);
                if (potentialHolsters.Count == 0)
                {
                    var backHolster = new GameObject("BackHolster");
                    backHolster.transform.position = new Vector3(-0.072f, 1.386f, -0.2006302f); // from editor fiddling
                    backHolster.transform.rotation = Quaternion.Euler(75, 90, 0);
                    backHolster.transform.SetParent(instance.transform);
                    potentialHolsters.Add(backHolster.transform);
                }

                var potentialHolstersIterator = potentialHolsters.GetEnumerator();
                potentialHolstersIterator.MoveNext();

                // instantiate weapons
                foreach (var weapon in weapons)
                {
                    var weaponInstance = Instantiate(weapon.prefab, triggerHandTransform);
                    var type = weapon.inventorySettings.type;

                    var controller = weaponInstance.GetComponent<WeaponController>();
                    controller.weaponSettings = weapon;
                    var item = WeaponUser.GetCorrectiveTransformsFromAsset(new WeaponUser.WeaponItem(controller, type));

                    WeaponUser.ApplyTransformationsToWeapon(item); // in case the thing has no holster
                    
                    var potentialHolster = potentialHolstersIterator.Current;
                    if (potentialHolster != null)
                    {
                        var holster = new WeaponUser.WeaponHolster(potentialHolster, type, item);
                        WeaponUser.ApplyTransformationsToWeapon(holster);
                        weaponUserComponent.holsters.Add(holster);
                        potentialHolstersIterator.MoveNext();
                    }
                    
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
                
                potentialHolstersIterator.Dispose();

                // initialize ik controller
                var ikBehaviourTree = instance.GetOrElseAddComponent<BehaviorTree>();
                ikBehaviourTree.ExternalBehavior = ikControlBehaviorTree;
                ikBehaviourTree.BehaviorName = "IKController";
                ikBehaviourTree.SetVariableValue("Self", instance.gameObject);
                
                return new Soldier()
                {
                    gameObject = instance,
                    transform = instance.transform,
                    aimComponentWithIK = aimComponentWithIK,
                    animator = animator,
                    navigation = navigation,
                    weaponUserComponent = weaponUserComponent,
                    attributes = stats
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
    }

}