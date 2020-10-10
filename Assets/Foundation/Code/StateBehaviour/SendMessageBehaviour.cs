using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AnimatorTransition;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SendMessageBehaviour : StateBehaviour {
    public enum ParamType {
        None, Int, Float, String
    }

    public string msgName;
    public ParamType type;
    public float floatValue;
    public int intValue;
    public string strValue;

    public float delay = 0;

    public override void Enter() {
        StartCoroutine(ActionSendMessage());
    }

    IEnumerator ActionSendMessage() {
        if (delay != 0)
            yield return new WaitForSeconds(delay);


        var o = SendMessageOptions.DontRequireReceiver;
        if (type == ParamType.None)
            runtime.SendMessage(msgName, o);
        else if (type == ParamType.Int)
            runtime.SendMessage(msgName, intValue, o);
        else if (type == ParamType.Float)
            runtime.SendMessage(msgName, floatValue, o);
        else if (type == ParamType.String)
            runtime.SendMessage(msgName, strValue, o);
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(SendMessageBehaviour))]
public class SendMessageBehaviourInspector : Editor {

    public override void OnInspectorGUI() {
        var b = (SendMessageBehaviour) target;
        EditorGUI.BeginChangeCheck();

        b.msgName = EditorGUILayout.TextField("Message", b.msgName);
        b.type = (SendMessageBehaviour.ParamType) EditorGUILayout.EnumPopup("Param Type", b.type);

        if (b.type == SendMessageBehaviour.ParamType.Float) {
            b.floatValue = EditorGUILayout.FloatField("Value", b.floatValue);
        } else if (b.type == SendMessageBehaviour.ParamType.Int) {
            b.intValue = EditorGUILayout.IntField("Value", b.intValue);
        } else if (b.type == SendMessageBehaviour.ParamType.String) {
            b.strValue = EditorGUILayout.TextField("Value", b.strValue);
        }

        b.delay = EditorGUILayout.FloatField("Delay", b.delay);

        if (EditorGUI.EndChangeCheck()) {
            EditorUtility.SetDirty(b);
        }
    }

}

#endif