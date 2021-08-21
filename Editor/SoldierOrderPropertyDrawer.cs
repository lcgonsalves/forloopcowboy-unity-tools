using System;
using ForLoopCowboyCommons.Agent.CustomOrders;
using ForLoopCowboyCommons.EditorHelpers;
using UnityEditor;
using UnityEngine;

namespace ForLoopCowboyCommons.Editor
{
    [CustomPropertyDrawer(typeof(SoldierControlStep))]
    public class SoldierOrderPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var step = PropertyDrawerUtil.GetValue<SoldierControlStep>(property);

            // add dropdown for switching the action types
            var actionTypeSerialized = property.FindPropertyRelative("actionType");
            var actionTypeDropdownHeight = EditorGUI.GetPropertyHeight(actionTypeSerialized);
            
            EditorGUI.PropertyField(
                new Rect(
                    position.x,
                    position.y,
                    position.width * 0.95f,
                    actionTypeDropdownHeight
                    ), actionTypeSerialized);

            switch (step.actionType)
            {
                case SoldierControlStep.ControlOptions.FollowNearestPath:
                    EditorGUI.PropertyField(
                        new Rect(
                            position.x,
                            position.y + actionTypeDropdownHeight + 5f,
                            position.width,
                            position.height - actionTypeDropdownHeight
                            ), property.FindPropertyRelative("followPathSettings"), true);
                    break;
                case SoldierControlStep.ControlOptions.Idle:
                    // idle has no extra settings
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var step = PropertyDrawerUtil.GetValue<SoldierControlStep>(property);
            float settingsHeight = base.GetPropertyHeight(property, label);
            
            switch (step.actionType)
            {
                case SoldierControlStep.ControlOptions.FollowNearestPath:
                    settingsHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("followPathSettings"), true);
                    break;
                default:
                    return settingsHeight;
            }

            return settingsHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("actionType"));

        }
    }
}