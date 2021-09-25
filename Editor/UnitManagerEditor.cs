using forloopcowboy_unity_tools.Scripts.GameLogic;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    [CustomEditor(typeof(UnitManager))]
    public class UnitManagerEditor : UnityEditor.Editor
    {
        private GameObject toSpawn;
        private UnitManager.Side spawnSide;
        private UnitManager.SpawnType spawnType;
        public override void OnInspectorGUI()
        {
            var manager = (UnitManager) target;
            DrawDefaultInspector();
            
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            toSpawn = (GameObject) EditorGUILayout.ObjectField(toSpawn, typeof(GameObject), allowSceneObjects: true);
            spawnSide = (UnitManager.Side) EditorGUILayout.EnumPopup(spawnSide);
            spawnType = (UnitManager.SpawnType) EditorGUILayout.EnumPopup(spawnType);
            if (GUILayout.Button("Spawn"))
            {
                manager.Spawn(spawnSide, toSpawn, spawnType);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}