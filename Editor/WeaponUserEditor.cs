using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    [CustomEditor(typeof(WeaponUser))]
    public class WeaponUserEditor : UnityEditor.Editor
    {
        private static bool showInventory = false;
        private static bool showHolsters = false;
        private static int switchWeaponIndex = 0;

        private List<WeaponUser.WeaponItem> weaponsInHolsters = new List<WeaponUser.WeaponItem>();
        private List<WeaponUser.WeaponItem> weaponsUnholstered = new List<WeaponUser.WeaponItem>();

        public override void OnInspectorGUI()
        {

            // each weapon item should have a button to
            // 1: put in hand
            // 2: put in holster
            // 3: apply local transformations
            
            // each holster should have a button to:
            // 1: apply holstered weapon local transforms

            WeaponUser user = (WeaponUser) target;

            // reset each time
            weaponsInHolsters.Clear();
            weaponsUnholstered.Clear();
            
            foreach (var weapon in user.inventory)
            {
                if (user.holsters.Exists(aHolster => aHolster.Contains(weapon)))
                    weaponsInHolsters.Add(weapon);
                else weaponsUnholstered.Add(weapon);
            }
            
            // button to switch weapons
            EditorGUILayout.BeginHorizontal();
            var names = weaponsInHolsters.Select(_ => _.ToString()).ToArray();
            switchWeaponIndex = EditorGUILayout.Popup(
                "Switch weapon to: ",
                switchWeaponIndex,
                names
            );
            if (GUILayout.Button("Select"))
            {
                user.HolsterActive();
                user.EquipWeapon(weaponsInHolsters[switchWeaponIndex]);
            }

            if (GUILayout.Button("Unequip"))
            {
                user.HolsterActive();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // hand transform
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerHandTransform"));
            
            // Inventory
            showInventory = EditorGUILayout.Foldout(showInventory, "Weapon Inventory");
            if (showInventory)
            {
                GUILayout.BeginVertical(); // weapons

                var serializedInventory = serializedObject.FindProperty("inventory");
                for (int i = 0; i < serializedInventory.arraySize; i++)
                {
                    var weaponItem = user.inventory[i];
                    DrawWeaponItem(weaponItem, serializedInventory.GetArrayElementAtIndex(i));
                }
            
                if (GUILayout.Button("Add weapon to inventory"))
                {
                    var obj = new WeaponUser.WeaponItem(null, WeaponUser.WeaponType.Primary);
                    
                    user.inventory.Add(obj);
                    weaponsUnholstered.Add(obj);
                }
            
                GUILayout.EndVertical(); // weapons end
            }
            
            // Holsters
            showHolsters = EditorGUILayout.Foldout(showHolsters, "Holsters");
            if (showHolsters)
            {
                GUILayout.BeginVertical(); // holsters

                var serializedHolsters = serializedObject.FindProperty("holsters");
                for (int i = 0; i < serializedHolsters.arraySize; i++)
                {
                    var weaponItem = user.holsters[i];
                    DrawHolster(weaponItem, serializedHolsters.GetArrayElementAtIndex(i));
                }
            
                if (GUILayout.Button("Add new holster"))
                {
                    user.holsters.Add(new WeaponUser.WeaponHolster());
                }
            
                GUILayout.EndVertical(); // holsters end
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorSettings"));
            
            if (GUI.changed) EditorUtility.SetDirty(target);
            
            serializedObject.ApplyModifiedProperties();
            
            // DrawDefaultInspector();
        }

        private void DrawHolster(WeaponUser.WeaponHolster holster, SerializedProperty serializedHolster)
        {
            WeaponUser user = (WeaponUser) target;
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name: " + (holster.holsterTransform != null ? holster.holsterTransform.transform.name : "No game object referenced."), EditorStyles.boldLabel);
            if (GUILayout.Button("x", GUILayout.Height(20), GUILayout.Width(20)))
            {
                ((WeaponUser) target).holsters.Remove(holster);
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedHolster.FindPropertyRelative("type"));
            
            GUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedHolster.FindPropertyRelative("holsterTransform"), new GUIContent("Game object: "));
            
            GUILayout.BeginHorizontal();

            var items = new List<WeaponUser.WeaponItem?>(weaponsUnholstered.Count + 1);
            if (holster.content != null)
            {
                // get from list to get most updated value
                var updatedHolsterContent = weaponsInHolsters.Find(_ => _.Equals(holster.content));
                items.Add(updatedHolsterContent);
            }
            foreach (var item in weaponsUnholstered) items.Add(item);
            items.Add(new WeaponUser.WeaponItem()); // empty

            var names = items
                .Select(_ => _.ToString())
                .ToArray();

            var selected = EditorGUILayout.Popup(
                "Weapon Item: ",
                0, // we always append the holster's current weapon to the beginning.
                names
            );

            var selectedItem = items[selected];

            if (selectedItem != null)
            {
                WeaponUser.WeaponItem selectedWeaponItem = (WeaponUser.WeaponItem) selectedItem;
                if (holster.content != null && !selectedItem.Equals(holster.content))
                {
                    weaponsUnholstered.Remove(selectedWeaponItem);
                    weaponsInHolsters.Add(selectedWeaponItem);

                    holster.content = selectedWeaponItem;
                    EditorUtility.SetDirty(target);
                }
            }
            
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("When in hand:",  EditorStyles.boldLabel);
            var translationProp = serializedHolster.FindPropertyRelative("_correctiveTranslation");
            var rotationProp = serializedHolster.FindPropertyRelative("_correctiveRotation");

            GUI.enabled = holster.content is { };
            EditorGUILayout.PropertyField(translationProp);
            EditorGUILayout.PropertyField(rotationProp);

            
            GUILayout.BeginHorizontal(); // btns

            var applySettingsToTransform = GUILayout.Button("Apply to Transform");
            var updateSetings = GUILayout.Button("Get from Transform");
            var transform = holster.weaponTransform;
            
            if (GUILayout.Button("Reset"))
            {
                if (transform is { })
                {
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                }
            }
            
            GUILayout.EndHorizontal(); // btns
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Holster weapon"))
            {
                WeaponUser.HolsterWeapon(holster);
            }
            
            if (GUILayout.Button("Equip weapon"))
            {
                user.EquipWeapon(holster);
            }
            
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();

            if (applySettingsToTransform)
            {
                WeaponUser.ApplyTransformationsToWeapon(holster);
            }

            if (updateSetings)
            {
                if (transform is { })
                {
                    WeaponUser.GetTransformsFromWeapon(holster);
                    
                    translationProp.vector3Value = transform.localPosition;
                    rotationProp.vector3Value = transform.localRotation.eulerAngles;
                }
            }
        }

        private void DrawWeaponItem(WeaponUser.WeaponItem weaponItem, SerializedProperty serializedWeaponItem)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name: " + (weaponItem.weapon?.transform.name ?? "No game object referenced."), EditorStyles.boldLabel);
            if (GUILayout.Button("x", GUILayout.Height(20), GUILayout.Width(20)))
            {
                var user = (WeaponUser) target;
                
                user.inventory.Remove(weaponItem);
                foreach (var holster in user.holsters)
                {
                    if (holster.Contains(weaponItem))
                    {
                        holster.content = null;
                        break;
                    }
                }
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedWeaponItem.FindPropertyRelative("type"));
            
            GUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedWeaponItem.FindPropertyRelative("weapon"), new GUIContent("Game object: "));
            
            GUILayout.Space(8);
            GUILayout.Label("When in hand:",  EditorStyles.boldLabel);
            var translationProp = serializedWeaponItem.FindPropertyRelative("_correctiveTranslation");
            var rotationProp = serializedWeaponItem.FindPropertyRelative("_correctiveRotation");

            GUI.enabled = weaponItem.weapon is { };
            EditorGUILayout.PropertyField(translationProp);
            EditorGUILayout.PropertyField(rotationProp);

            
            GUILayout.BeginHorizontal(); // btns

            var applySettingsToTransform = GUILayout.Button("Apply to Transform");
            var getSettingsFromTransform = GUILayout.Button("Get from Transform");

            var transform = weaponItem.weapon?.transform;
            
            if (GUILayout.Button("Reset"))
            {
                if (weaponItem.weapon is { })
                {
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                }
            }
            
            GUI.enabled = true;
                
            GUILayout.EndHorizontal(); // btns
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply to Asset"))
            {
                var weaponNotNull = weaponItem.weapon is { };
                var settingsNotNUll = weaponNotNull && weaponItem.weapon.weaponSettings;
                
                if (!weaponNotNull) Debug.LogWarning("Weapon is null.");
                if (!settingsNotNUll) Debug.LogWarning("Settings is null.");
                
                if (weaponNotNull && settingsNotNUll) 
                    weaponItem.weapon.weaponSettings.inventorySettings = weaponItem;
            }

            WeaponUser wpnUser = (WeaponUser) target;
            if (GUILayout.Button("Get from Asset"))
            {
                wpnUser.GetCorrectiveTransformsFromAsset(weaponItem);
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();

            if (applySettingsToTransform)
            {
                WeaponUser.ApplyTransformationsToWeapon(weaponItem);
            }

            if (getSettingsFromTransform)
            {
                if (weaponItem.weapon is { })
                {
                    WeaponUser.GetTransformsFromWeapon(weaponItem);
                    
                    translationProp.vector3Value = transform.localPosition;
                    rotationProp.vector3Value = transform.localRotation.eulerAngles;
                }
            }

        }


    }

    public static class ExtendedWeaponUser
    {

        public static WeaponUser.WeaponItem GetCorrectiveTransformsFromAsset(this WeaponUser user, WeaponUser.WeaponItem weaponItem)
        {
            var weaponNotNull = weaponItem.weapon is { };
            var settingsNotNUll = weaponNotNull && weaponItem.weapon.weaponSettings;
                
            if (!weaponNotNull) Debug.LogWarning("Weapon is null.");
            if (!settingsNotNUll) Debug.LogWarning("Settings is null.");

            if (weaponNotNull && settingsNotNUll)
            {
                var presetSettings = weaponItem.weapon.weaponSettings.inventorySettings;
                    
                weaponItem.correctiveTranslation = presetSettings.correctiveTranslation;
                weaponItem.correctiveRotation = presetSettings.correctiveRotation;
            }

            return weaponItem;
        }

    }
}