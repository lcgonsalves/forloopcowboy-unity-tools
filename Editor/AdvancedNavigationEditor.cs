using UnityEditor;
using UnityEngine;
using UnityTemplateProjects.forloopcowboy_unity_tools.Scripts.Soldier;

namespace ForLoopCowboyCommons.Editor
{
    [CustomEditor(typeof(AdvancedNavigation))]
    public class AdvancedNavigationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AdvancedNavigation navigation = (AdvancedNavigation) target;
            
            DrawDefaultInspector();

            if (navigation.state?.nextTarget is { })
                GUILayout.Label("Next target: " + navigation.state?.nextTarget.name);

            if (GUILayout.Button("⏸Pause"))
            {
                navigation.Pause();
            }

            if (GUILayout.Button("▶ Resume"))
            {
                navigation.Resume();
            }
        }
    }
}