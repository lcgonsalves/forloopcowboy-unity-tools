using System.Linq;
using forloopcowboy_unity_tools.Scripts.Bullet;
using forloopcowboy_unity_tools.Scripts.Spells.Implementations.Projectile;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    public class BulletSystemMonitor : EditorWindow
    {
        [MenuItem("BulletSystem/View bullet system")]
        private static void ShowWindow()
        {
            var window = GetWindow<BulletSystemMonitor>();
            window.titleContent = new GUIContent("Bullet System Monitor");
            window.Show();
        }

        private void OnGUI()
        {
            var pools = BulletSystem.GetPools;

            using var enumerator = pools.GetEnumerator();
            enumerator.MoveNext();
            
            GUILayout.Label("All active bullets");
            
            while (enumerator.Current != null && EditorApplication.isPlaying)
            {
                var pool = enumerator.Current;
                var allActive = pool.all.Where(_ => _.gameObject.activeInHierarchy).ToArray();

                if (allActive.Length == 0)
                {
                    enumerator.MoveNext();
                    continue;
                }

                var first = allActive[0];
                
                GUILayout.Label(first.Settings.name);
                
                for (int i = 0; i < allActive.Length; i++)
                {
                    EditorGUILayout.ObjectField(allActive[i], typeof(BulletController), true);
                }

                enumerator.MoveNext();
            }
        }
    }
}