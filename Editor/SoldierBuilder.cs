using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
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
        private ExternalBehaviorTree _behaviorTree;
        private GameObject _characterRigPrefab;
        private List<Weapon> _weapons = new List<Weapon>(1);
        private AnimatorController _animatorController;
        private Transition _aimTransition, _ikLerpInTransition, _ikLerpOutTransition;
        private StringList _firstNames, _lastNames;

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

            GUI.enabled = _behaviorTree && _characterRigPrefab && _weapons.Count > 0 && _animatorController && _ikLerpInTransition && _ikLerpInTransition && _aimTransition;
            if (GUILayout.Button("Instantiate"))
            {
                var instance = SoldierRandomizer.InstantiateCharacter(
                    _characterRigPrefab,
                    _animatorController,
                    _behaviorTree,
                    _aimTransition,
                    _ikLerpInTransition,
                    _ikLerpOutTransition,
                    _firstNames,
                    _lastNames
                )(_weapons, Vector3.zero);

                Selection.activeGameObject = instance.gameObject;
            }

        }
        
    }
    
}