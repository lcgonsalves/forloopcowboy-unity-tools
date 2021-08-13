using System;
using System.Collections.Generic;
using ForLoopCowboyCommons.Agent;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForLoopCowboyCommons.Editor
{
    [CustomEditor(typeof(OrderSettings))]
    public class OrderSettingsEditor : UnityEditor.Editor
    {
        
        private SerializedObject _getTarget;
        private SerializedProperty _serializedActionSteps;
        private SerializedProperty _serializedWaitSteps;
        
        private void OnEnable()
        {
            OrderSettings o = (OrderSettings) target;
            
            _getTarget = new SerializedObject(o);
            _serializedActionSteps = _getTarget.FindProperty("actionSteps");
            _serializedWaitSteps = _getTarget.FindProperty("waitSteps");
        }

        public override void OnInspectorGUI()
        {
            _getTarget.Update();
            OrderSettings o = (OrderSettings) target;
            
            // btn definitions

            bool addActionStep = GUILayout.Button("Add action step");
            bool addWaitStep = GUILayout.Button("Add wait step");

            foreach (var step in o.iterator)
            {
                var serializedStep = (SerializedProperty) null;

                switch (step)
                {
                    case WaitStep _:
                    {
                        serializedStep = _serializedWaitSteps.GetArrayElementAtIndex(step.localIndex);
                        var waitStep = serializedStep?.FindPropertyRelative("waitTimeInSeconds");
                        if (waitStep != null) DrawStep(step, serializedStep, waitStep);
                        break;
                    }
                    case ExecuteUnityEventsStep _:
                    {
                        serializedStep = _serializedActionSteps.GetArrayElementAtIndex(step.localIndex);
                        var actionStep = serializedStep?.FindPropertyRelative("actions");
                        if (actionStep != null) DrawStep(step, serializedStep, actionStep);
                        break;
                    }
                    default:
                        Debug.LogError($"Step type {step.GetType()} isn't properly handled!");
                        break;
                }
            }
            
            if (addActionStep) AddNewStep(new ExecuteUnityEventsStep());
            if (addWaitStep) AddNewStep(new WaitStep(1f));
            

            if (GUI.changed)
            {
                EditorUtility.SetDirty(o);
            }
            
            _getTarget.ApplyModifiedProperties();
            
        }

        private void AddNewStep(OrderStep s)
        {
            OrderSettings o = (OrderSettings) target;
            
            s.globalIndex = o.iterator.Count + 1;
            o.Add(s);
        }

        private void DrawStep(OrderStep step, SerializedProperty serializedStep, SerializedProperty prop)
        {
            DrawStep(step, serializedStep, new []{prop});
        }
        private void DrawStep(OrderStep step, SerializedProperty serializedStep, IEnumerable<SerializedProperty> serializedStepProperties)
        {
            // container for step
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedStep.FindPropertyRelative("name"));
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            // class display
            foreach (var prop in serializedStepProperties)
            {
                EditorGUILayout.PropertyField(prop, new[] {GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.85f)});
            }

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
            GUILayout.EndVertical();
            
            OrderSettings o = (OrderSettings) target;

            if (moveUp) o.UpdateGlobalIndexOf(step, step.globalIndex - 1);
            if (moveDown) o.UpdateGlobalIndexOf(step, step.globalIndex + 1);
            if (delete) o.Remove(step);

        }
    }
}