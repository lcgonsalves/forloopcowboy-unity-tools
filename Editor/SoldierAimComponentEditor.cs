using forloopcowboy_unity_tools.Scripts.Soldier;
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
        }
    }
}