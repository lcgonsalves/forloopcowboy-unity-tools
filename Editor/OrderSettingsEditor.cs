using System;
using System.Collections.Generic;
using ForLoopCowboyCommons.Agent;
using ForLoopCowboyCommons.Agent.CustomOrders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForLoopCowboyCommons.Editor
{
    [CustomEditor(typeof(OrderSettings))]
    public class OrderSettingsEditor : UnityEditor.Editor
    {
        
        private SerializedObject _getTarget;
        private SerializedProperty _serializedUnityEventsSteps;
        private SerializedProperty _serializedWaitSteps;
        private SerializedProperty _serializedSoldierSteps;

        private void OnEnable()
        {
            OrderSettings o = (OrderSettings) target;
            
            _getTarget = new SerializedObject(o);
            _serializedUnityEventsSteps = _getTarget.FindProperty("actionSteps");
            _serializedWaitSteps = _getTarget.FindProperty("waitSteps");
            _serializedSoldierSteps = _getTarget.FindProperty("soldierSteps");
        }

        public override void OnInspectorGUI()
        {
            _getTarget.Update();
            OrderSettings o = (OrderSettings) target;
            
            // btn definitions

            bool addActionStep = GUILayout.Button("Add action step");
            bool addWaitStep = GUILayout.Button("Add wait step");
            bool addSoldierStep = GUILayout.Button("Add soldier puppeteering step");

            foreach (var step in o.iterator)
            {
                var serializedStep = (SerializedProperty) null;

                switch (step)
                {
                    case SoldierControlStep _:
                        serializedStep = _serializedSoldierSteps.GetArrayElementAtIndex(step.localIndex);
                        
                        // soldier step has a custom property drawer.
                        DrawStep(step, serializedStep, serializedStep);
                        
                        break;
                    
                    case Order.WaitStep _:
                    {
                        serializedStep = _serializedWaitSteps.GetArrayElementAtIndex(step.localIndex);
                        var waitStep = serializedStep?.FindPropertyRelative("waitTimeInSeconds");
                        if (waitStep != null) DrawStep(step, serializedStep, waitStep);
                        break;
                    }
                    case Order.ExecuteUnityEventsStep _:
                    {
                        serializedStep = _serializedUnityEventsSteps.GetArrayElementAtIndex(step.localIndex);
                        var actionStep = serializedStep?.FindPropertyRelative("actions");
                        if (actionStep != null) DrawStep(step, serializedStep, actionStep);
                        break;
                    }
                    
                    default:
                        Debug.LogError($"Step type {step.GetType()} isn't properly handled!");
                        break;
                }
            }
            
            if (addActionStep) AddNewStep(new Order.ExecuteUnityEventsStep());
            if (addWaitStep) AddNewStep(new Order.WaitStep(1f));
            if (addSoldierStep) AddNewStep(new SoldierControlStep());
            

            if (GUI.changed)
            {
                EditorUtility.SetDirty(o);
            }
            
            _getTarget.ApplyModifiedProperties();

        }

        private void AddNewStep(Order.Step s)
        {
            OrderSettings o = (OrderSettings) target;
            
            s.globalIndex = o.iterator.Count + 1;
            o.Add(s);
        }

        private void DrawStep(Order.Step step, SerializedProperty serializedStep, SerializedProperty prop)
        {
            DrawStep(step, serializedStep, new []{prop});
        }
        private void DrawStep(Order.Step step, SerializedProperty serializedStep, IEnumerable<SerializedProperty> serializedStepProperties)
        {
            // container for step
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            
            // adding some breathing room for the text field
            GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(serializedStep.FindPropertyRelative("name"));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);

            // class display
            // breathing room on left side and top/bottom
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Space(20);
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            foreach (var prop in serializedStepProperties)
            {
                EditorGUILayout.PropertyField(prop, true, new[] {GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.70f)});
            }
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.Space(2);
            GUILayout.EndHorizontal();

            var btnSettings = new[]
            {
                GUILayout.Width(22),
                GUILayout.Height(22)
            };

            GUILayout.FlexibleSpace();

            // action buttons buttons
            GUILayout.BeginVertical();
            bool moveUp = GUILayout.Button("↑", btnSettings);
            bool moveDown = GUILayout.Button("↓", btnSettings);
            bool delete = GUILayout.Button("x", btnSettings);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
            
            OrderSettings o = (OrderSettings) target;

            if (moveUp) o.UpdateGlobalIndexOf(step, step.globalIndex - 1);
            if (moveDown) o.UpdateGlobalIndexOf(step, step.globalIndex + 1);
            if (delete) o.Remove(step);
        }

    }
}