using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EGL = UnityEditor.EditorGUILayout;
using GL = UnityEngine.GUILayout;
using AnimatorTransition;

[CustomEditor(typeof(TransitionRuntime))]
public class TransitionRuntimeInspector : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var rt = (TransitionRuntime) target;
        if (Application.isPlaying) {
            foreach (var p in rt.paramTable) {
                if (p.type == ParamType.Bool)
                    EGL.Toggle(p.name + "(bool)", p.boolValue);
                else if (p.type == ParamType.Trigger)
                    EGL.Toggle(p.name + "(trigger)", p.boolValue);
                else if (p.type == ParamType.Float)
                    EGL.LabelField(p.name + " (float)", p.floatValue.ToString());
                else if (p.type == ParamType.Int)
                    EGL.LabelField(p.name + "(int)", p.intValue.ToString());
            }

            Repaint();
        } else {
            if (GUILayout.Button("Edit", GUILayout.MaxWidth(150))) {
                var instance = EditorWindow.GetWindow<TransitionProfileEditor>();
                instance.titleContent = new GUIContent("Transition Prof.");
                instance.BeginEdit(rt.profile);
            }
        }
    }
}