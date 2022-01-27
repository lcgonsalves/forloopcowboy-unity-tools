using forloopcowboy_unity_tools.Scripts.Environment;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    [CustomEditor(typeof(WaypointNode)), CanEditMultipleObjects]
    public class WaypointNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            WaypointNode self = (WaypointNode) target;
            DrawDefaultInspector();

            if (GUILayout.Button("Add and chain"))
            {
                
                var transform = self.transform;
                
                var clone = Instantiate(self, transform.position, transform.rotation, transform.parent);
                self.SetNext(clone);
                clone.name = self.name;
                Selection.activeGameObject = clone.gameObject;
            }

            if (GUILayout.Button("Select end of chain"))
            {
                Selection.activeGameObject = self.GetEnd()?.gameObject ?? self.gameObject;
            }
        }
        
        [MenuItem("Waypoints/New")]
        private static void InstantiateWaypoint()
        {
            var sample = new GameObject("Untitled Waypoint");
            sample.AddComponent<WaypointNode>();
        }
    }
}