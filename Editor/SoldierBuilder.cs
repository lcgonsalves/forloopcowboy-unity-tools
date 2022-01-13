using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
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
        private ExternalBehaviorTree _ikBehaviorTree;
        private ExternalBehaviorTree _combatBehaviorTree;
        private BulletImpactSettings _bulletImpactSettings;
        private GameObject _characterRigPrefab;
        private List<Weapon> _weapons = new List<Weapon>(1);
        private AnimatorController _animatorController;
        private Transition _aimTransition, _ikLerpInTransition, _ikLerpOutTransition;
        private StringList _firstNames, _lastNames;
        private NPCSettings _npcSettings;

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
            _ikBehaviorTree = (ExternalBehaviorTree) EditorGUILayout.ObjectField(_ikBehaviorTree, typeof(ExternalBehaviorTree), false);
            
            GUILayout.Label("Select the Combat behavior controller:");
            _combatBehaviorTree = (ExternalBehaviorTree) EditorGUILayout.ObjectField(_combatBehaviorTree, typeof(ExternalBehaviorTree), false);

            GUILayout.Label("Select the Animator Controller:");
            _animatorController = (AnimatorController) EditorGUILayout.ObjectField(_animatorController, typeof(AnimatorController), false);

            GUILayout.Label("Select the bullet impact settings:");
            _bulletImpactSettings = (BulletImpactSettings) EditorGUILayout.ObjectField(_bulletImpactSettings, typeof(BulletImpactSettings), false);

            GUILayout.Label("Select the NPC settings:");
            _npcSettings = (NPCSettings) EditorGUILayout.ObjectField(_npcSettings, typeof(NPCSettings), false);
            
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
            
            GUILayout.Label("Select names to be randomized");
            _firstNames = (StringList) EditorGUILayout.ObjectField(_firstNames, typeof(StringList), false);
            _lastNames = (StringList) EditorGUILayout.ObjectField(_lastNames, typeof(StringList), false);
            
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

            GUI.enabled =
                _weapons.Count > 0 &&
                _characterRigPrefab &&
                _animatorController &&
                _ikBehaviorTree &&
                _combatBehaviorTree &&
                _bulletImpactSettings &&
                _aimTransition &&
                _ikLerpInTransition &&
                _ikLerpOutTransition &&
                _firstNames &&
                _lastNames &&
                _npcSettings;
            
            if (GUILayout.Button("Instantiate"))
            {
                var instance = SoldierRandomizer.InstantiateCharacter(
                    _characterRigPrefab,
                    _animatorController,
                    _ikBehaviorTree,
                    _combatBehaviorTree,
                    _bulletImpactSettings,
                    _aimTransition,
                    _ikLerpInTransition,
                    _ikLerpOutTransition,
                    _firstNames,
                    _lastNames,
                    _npcSettings,
                    FindObjectOfType<GameplayManager>() // *shrug*
                )(_weapons, Vector3.zero);

                Selection.activeGameObject = instance.gameObject;
            }

        }
        
    }
    
}