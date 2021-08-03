using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Transition))]
public class TransitionEditor : Editor
{
    public override void OnInspectorGUI()
    {

        EditorUtility.SetDirty(target);

        EditorGUILayout.Space(1);
        Transition t = (Transition) target;

        if (t.transition == null) t.transition = new AnimationCurve();

        EditorGUILayout.LabelField("Transition Properties", EditorStyles.boldLabel);
        EditorGUILayout.Space(1);

        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Duration");
            t.duration = EditorGUILayout.Slider(t.duration, 0.01f, 10f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Amplitude (max value)");
            t.amplitude = EditorGUILayout.Slider(t.amplitude, 0.01f, 10f);
        GUILayout.EndHorizontal();

        t.transition = EditorGUILayout.CurveField(t.transition, Color.green, new Rect(0,0, 1, t.amplitude));

        EditorGUILayout.Space(1);

    }
}