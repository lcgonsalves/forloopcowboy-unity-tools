using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using forloopcowboy_unity_tools.Scripts.Weapon;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    public class SoldierBuilder : EditorWindow
    {
        private ExternalBehaviorTree _behaviorTree;
        private GameObject _characterRigPrefab;
        private List<Weapon> _weapons = new List<Weapon>(1);
        private AnimatorController _animatorController;
        private Transition _aimTransition, _ikLerpInTransition, _ikLerpOutTransition;

        /// <summary>
        /// Returns a function that generates the character prefab, given a set of weapons.
        /// </summary>
        /// <param name="characterRigPrefab">Character with rig</param>
        /// <param name="animatorController">Animator controller with animations compatible with the other components.</param>
        /// <param name="ikControlBehaviorTree">Behaviour tree for controlling IK</param>
        /// <param name="aimTransition">Transition lerp for aiming</param>
        /// <param name="ikLerpInTransition"></param>
        /// <param name="ikLerpOutTransition"></param>
        /// <returns></returns>
        public static Func<List<Weapon>, GameObject> InstantiateCharacter(
            GameObject characterRigPrefab,
            AnimatorController animatorController,
            ExternalBehaviorTree ikControlBehaviorTree,
            Transition aimTransition,
            Transition ikLerpInTransition,
            Transition ikLerpOutTransition
        )
        {
            return weapons =>
            {
                var instance = Instantiate(characterRigPrefab);

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
                    var item = weaponUserComponent.GetCorrectiveTransformsFromAsset(new WeaponUser.WeaponItem(controller, type));

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
                return instance;
            };
        }
        
        [MenuItem("NPC/Soldier Builder")]
        private static void ShowWindow()
        {
            var window = GetWindow<SoldierBuilder>();
            window.titleContent = new GUIContent("Soldier Builder");
            window.Show();
        }

        private void OnGUI()
        {

            GUILayout.Label("First select the character rig prefab:");
            _characterRigPrefab =
                (GameObject) EditorGUILayout.ObjectField(_characterRigPrefab, typeof(GameObject), false);
            
            GUILayout.Label("Select the IK controller:");
            _behaviorTree = (ExternalBehaviorTree) EditorGUILayout.ObjectField(_behaviorTree, typeof(ExternalBehaviorTree), false);
            
            GUILayout.Label("Select the Animator Controller:");
            _animatorController = (AnimatorController) EditorGUILayout.ObjectField(_animatorController, typeof(AnimatorController), false);

            GUILayout.Label("Select the following transitions:");
            
            EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Aim transition");
                _aimTransition = (Transition) EditorGUILayout.ObjectField(_aimTransition, typeof(Transition), false);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
                GUILayout.Label("IK Lerp In transition");
                _ikLerpInTransition = (Transition) EditorGUILayout.ObjectField(_ikLerpInTransition, typeof(Transition), false);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
                GUILayout.Label("IK Lerp Out transition");
                _ikLerpOutTransition = (Transition) EditorGUILayout.ObjectField(_ikLerpOutTransition, typeof(Transition), false);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.Label("Select the weapons:");
            int deleteIndex = -1;
            for (int i = 0; i < _weapons.Count; i++)
            {
                GUILayout.BeginHorizontal();
                _weapons[i] = (Weapon) EditorGUILayout.ObjectField(_weapons[i], typeof(Weapon));
                deleteIndex = GUILayout.Button("X") ? i : -1;
                GUILayout.EndHorizontal();
            }

            if (deleteIndex >= 0)
            {
                _weapons.RemoveAt(deleteIndex);
            }

            if (GUILayout.Button("+ Add weapon"))
            {
                _weapons.Add(null);
            }
            
            GUILayout.Space(15);

            GUI.enabled = _behaviorTree && _characterRigPrefab && _weapons.Count > 0 && _animatorController && _ikLerpInTransition && _ikLerpInTransition && _aimTransition;
            if (GUILayout.Button("Instantiate"))
            {
                var instance = InstantiateCharacter(_characterRigPrefab, _animatorController, _behaviorTree, _aimTransition, _ikLerpInTransition, _ikLerpOutTransition)(_weapons);

                Selection.activeGameObject = instance.gameObject;
            }

        }
        
    }
}