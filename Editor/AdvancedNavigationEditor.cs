using System;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.Environment;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEditor;
using UnityEngine;

namespace forloopcowboy_unity_tools.Editor
{
    [CustomEditor(typeof(AdvancedNavigation))]
    public class AdvancedNavigationEditor : UnityEditor.Editor
    {
        private bool showControls = false;
        private WaypointNode forcedTarget = null;
        private int depth = 1;
        private float speed = 1f;
        private bool showTerminationMessage = false;
        private static bool showNavigationPath = false;
        
        public override void OnInspectorGUI()
        {
            AdvancedNavigation navigation = (AdvancedNavigation) target;

            GUILayout.Label("Manipulate Navigation", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox); // next target + play/pause
            
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Next target => ", new GUILayoutOption[]
                    {
                        GUILayout.MaxWidth(100)
                    });

                    EditorGUILayout.ObjectField(navigation.state?.nextTarget, typeof(WaypointNode), true);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Last visited => ", new GUILayoutOption[]
                    {
                        GUILayout.MaxWidth(100)
                    });
                    EditorGUILayout.ObjectField(navigation.LastVisited, typeof(WaypointNode), true);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Path started at => ", new GUILayoutOption[]
                    {
                        GUILayout.MaxWidth(100)
                    });
                    EditorGUILayout.ObjectField(navigation.LastWaypointPathStart, typeof(WaypointNode), true);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginVertical();
                    GUILayout.Label("Most recent path:", new GUILayoutOption[]
                    {
                        GUILayout.MaxWidth(180)
                    });
                    
                    GUILayout.BeginVertical(EditorStyles.helpBox, new []{GUILayout.MinHeight(30)});
                    
                    if (navigation.LastWaypointPath.Count == 0) GUILayout.Label("No recent path.");
                    else
                    {
                        showNavigationPath = EditorGUILayout.Foldout(showNavigationPath, "Nodes");
                        if (showNavigationPath)
                        {
                            for (int i = 0; i < navigation.LastWaypointPath.Count; i++)
                            {
                                var obj = navigation.LastWaypointPath[i];
                                var label = $"({i}.)";
                        
                                GUILayout.BeginHorizontal();
                        
                                GUILayout.Label(label);
                                EditorGUILayout.ObjectField(obj, typeof(WaypointNode), true);
                        
                                GUILayout.EndHorizontal();
                        
                            }
                        }
                    }
                    
                    GUILayout.EndVertical();
                    
                GUILayout.EndVertical();
                
                GUILayout.BeginHorizontal();
                    GUI.enabled = navigation.IsFollowingPath();
                    if (GUILayout.Button($"Pause"))
                    {
                        navigation.Pause();
                    }

                    GUI.enabled = navigation.IsAbleToResumeNavigating(true);
                    if (GUILayout.Button("Resume"))
                    {
                        navigation.Resume();
                    }
                    GUI.enabled = true;
                GUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical(); // next target + play/pause

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox); // set destination

            forcedTarget = (WaypointNode) EditorGUILayout.ObjectField("Set target: ",  forcedTarget, typeof(WaypointNode));
            depth = Mathf.Clamp(EditorGUILayout.IntField("Traversal depth: ", depth), 0, Int32.MaxValue);
            speed = Mathf.Clamp(EditorGUILayout.FloatField("Speed: ", speed), 0f, 20f);

            GUI.enabled = forcedTarget != null;

            // for exposing movement
            void FollowWaypointAndDisplayNotification()
            {
                navigation.FollowWaypointUntil(forcedTarget, speed, depth, () =>
                {
                    showTerminationMessage = true;
                    navigation.RunAsyncWithDelay(2f, () => showTerminationMessage = false);
                });
            }

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Simple Move"))
            {
                navigation.MoveToTransform(forcedTarget.transform, speed, 120f);
            }
            
            if (GUILayout.Button("Apply"))
            {
                FollowWaypointAndDisplayNotification();
            }

            if (GUILayout.Button("Apply & Clear"))
            {
                FollowWaypointAndDisplayNotification();
                forcedTarget = null;
                depth = Int32.MaxValue;
                speed = 1f;
            }

            if (GUILayout.Button("Clear"))
            {
                forcedTarget = null;
                depth = Int32.MaxValue;
                speed = 1f;
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical(); // destination setter
            
            GUILayout.Space(30);
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointSettings"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointReachedRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngularSpeed"));

            
            EditorGUILayout.Space(15);
            
            GUILayout.Label("Animator Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorUpdateSettings.enabled"), new GUIContent("Update animator state?"));
            GUI.enabled = navigation.animatorUpdateSettings.enabled;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorUpdateSettings.updateVelocity"));
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

        }
    }
}