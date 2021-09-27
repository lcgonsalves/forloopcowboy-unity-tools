using System.Collections.Generic;
using System.Linq;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Soldier;
using forloopcowboy_unity_tools.Scripts.Weapon;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    
    [CustomEditor(typeof(AimComponent), true)]
    public class SoldierAimComponentEditor : UnityEditor.Editor
    {
        Transform aimTarget = null;
        private bool aimGradually = false;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AimComponent refAimComponent = (AimComponent) target;
            
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Aim at ");
            aimTarget = (Transform) EditorGUILayout.ObjectField(aimTarget, typeof(Transform), true);
            aimGradually = EditorGUILayout.Toggle("Lerp?", aimGradually);
            GUI.enabled = aimTarget != null;
            if (GUILayout.Button(aimTarget != null ? "Go" : "Select a target"))
            {
                refAimComponent.Aim(aimTarget.position, aimGradually);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            DrawIKGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static int selectedIKSettingsToExport = 0;
        
        private void DrawIKGUI()
        {
            AimComponent refAimComponent = (AimComponent) target;

            EditorGUILayout.BeginHorizontal();
            if (refAimComponent is AimComponentWithIK ik)
            {
                var listOfAvailableSettings = ik.supportHandIKSettings?.Select(_ => _.forWeapon?.name ?? "None").ToArray();
                
                selectedIKSettingsToExport = EditorGUILayout.Popup(
                    selectedIKSettingsToExport < listOfAvailableSettings.Length ? selectedIKSettingsToExport : 0,
                    listOfAvailableSettings
                );

                GUI.enabled = ik.supportHandIKSettings != null;
                if (GUILayout.Button("Get from asset"))
                {
                    ik.GetIKSettingsFromAsset(selectedIKSettingsToExport);
                }
                
                if (GUILayout.Button("Apply to asset"))
                {
                    if (ik.supportHandIKSettings != null)
                    {
                        var selected = ik.supportHandIKSettings[selectedIKSettingsToExport];
                        selected.forWeapon.weaponSettings.ikSettings = new WeaponSavedIKSettings(selected);
                    }
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    public static class ExtendedSoldierAimIKComponent
    {
        public static void GetIKSettingsFromAsset(this AimComponentWithIK ik, int index)
        {
            var import = ik.weapon.weaponSettings.ikSettings;
            var settings = ik.supportHandIKSettings[index];
            
            settings.rotation.value = import.rotation.value;
            settings.translation.value = import.translation.value;
            
            settings.translation.weight = import.translation.weight;
            settings.rotation.weight = import.rotation.weight;
        }
    }
}