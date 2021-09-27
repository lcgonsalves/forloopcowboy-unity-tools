using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using forloopcowboy_unity_tools.Scripts.Weapon;
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

            GUI.enabled = _behaviorTree && _characterRigPrefab && _weapons.Count > 0 && _animatorController;
            if (GUILayout.Button("Instantiate"))
            {
                var instance = Instantiate(_characterRigPrefab);
                
                // initialize navigation
                var navigation = instance.GetOrElseAddComponent<AdvancedNavigation>();
                
                // initialize aim component
                var aimComponentWithIK = instance.GetOrElseAddComponent<AimComponentWithIK>();
                var animator = instance.GetComponent<Animator>();

                animator.runtimeAnimatorController = _animatorController;

                // initialize weapon user compoonent
                var weaponUserComponent = instance.GetOrElseAddComponent<WeaponUser>();
                var triggerHandTransform =
                    instance.transform.Find(
                        "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R");
                weaponUserComponent.triggerHandTransform = triggerHandTransform;
                weaponUserComponent.animatorSettings = new WeaponUser.AnimatorIntegrationSettings( true);

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
                            weaponUserComponent.animatorSettings.animatorParameters.Add(new WeaponUser.AnimatorIntegrationSettings.WeaponTypeAnimParams(weaponType));
                            break;
                    }
                }

                // instantiate weapons
                foreach (var weapon in _weapons)
                {
                    var weaponInstance = Instantiate(weapon.prefab, triggerHandTransform);
                    var type = weapon.inventorySettings.type;
                    
                    var controller = weaponInstance.GetComponent<WeaponController>();
                    controller.weaponSettings = weapon;
                    var item = weaponUserComponent.GetCorrectiveTransformsFromAsset(new WeaponUser.WeaponItem(controller, type));
                    var holster = new WeaponUser.WeaponHolster(instance.transform, type, item);
                    var ikSettings = new WeaponIKSettings();

                    ikSettings.limb = weapon.ikSettings.limb;
                    ikSettings.translation.value = weapon.ikSettings.translation.value;
                    ikSettings.rotation.value = weapon.ikSettings.rotation.value;
                    ikSettings.target = weaponInstance.transform.FindRecursively(weapon.ikSettings.path);
                    ikSettings.forWeapon = controller;
                    
                    WeaponUser.ApplyTransformationsToWeapon(holster);
                    
                    weaponUserComponent.inventory.Add(item);
                    weaponUserComponent.holsters.Add(holster);
                    aimComponentWithIK.supportHandIKSettings.Add(ikSettings);
                }
                
                // initialize ik controller
                var ikBehaviourTree = instance.GetOrElseAddComponent<BehaviorTree>();
                ikBehaviourTree.ExternalBehavior = _behaviorTree;
                ikBehaviourTree.BehaviorName = "IKController";

                Selection.activeGameObject = instance.gameObject;

            }

        }
    }
}