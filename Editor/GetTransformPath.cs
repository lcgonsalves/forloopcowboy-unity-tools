using forloopcowboy_unity_tools.Scripts.Core;
using UnityEditor;
using UnityEditor.TextCore.Text;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    public class GetTransformPath : EditorWindow
    {
        private Transform test;
        private string inputPath = "";
        Transform found = null;
        
        [MenuItem("GameObject/Get Transform Path")]
        private static void ShowWindow()
        {
            var window = GetWindow<GetTransformPath>();
            window.titleContent = new GUIContent("Transform Path");
            window.Show();
        }

        private void OnGUI()
        {
            var current = Selection.activeGameObject.transform;
            if (!current)
            {
                GUILayout.Label("Please select a transform.");
                return;
            }

            Transform topOfHierarchy = current;
            while (topOfHierarchy.parent)
                topOfHierarchy = topOfHierarchy.parent;
            
            var path = current.GetPathFrom(topOfHierarchy.name);

            EditorGUILayout.TextField(path);
            
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            test = ((GameObject) EditorGUILayout.ObjectField(test != null ? test.gameObject : null, typeof(GameObject),
                true))?.transform;
            
            inputPath = EditorGUILayout.TextField(inputPath);
            if (GUILayout.Button("Find"))
            {
                found = test.Find(inputPath);
            }

            EditorGUILayout.ObjectField(found, typeof(Transform));
            EditorGUILayout.EndHorizontal();
        }
        
    }
    
}