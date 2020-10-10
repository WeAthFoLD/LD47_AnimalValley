using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterController2D))]
public class CC2DEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (Application.isPlaying) {
            var cc = (CharacterController2D) target;

            GUI.enabled = false;

            EditorGUILayout.Vector2Field("速度", cc.velocity);
            EditorGUILayout.Vector2Field("位置", cc.position);
            EditorGUILayout.Toggle("着地", cc.grounded);
            EditorGUILayout.Toggle("右侧有地面", cc.rightHasGround);
            EditorGUILayout.Toggle("左侧有地面", cc.leftHasGround);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField(cc.horizontalCollisions.Count + " 横向碰撞");
                foreach (var hc in cc.horizontalCollisions) {
                    EditorGUILayout.ObjectField(hc.collider, typeof(Collider2D), allowSceneObjects: false);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField(cc.verticalCollisions.Count + " 纵向碰撞");
                foreach (var vc in cc.verticalCollisions) {
                    EditorGUILayout.ObjectField(vc.collider, typeof(Collider2D), allowSceneObjects: false);
                }
            }

            // EditorGUILayout.ObjectField("Moving Ground", (Component) cc.movingGround, typeof(Component), false);

            GUI.enabled = true;

            Repaint();
        }
    }
}