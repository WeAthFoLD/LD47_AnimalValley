using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class Sensor : MonoBehaviour {

    CollisionFilter[] filters;

    internal readonly HashSet<Collider2D> colliding = new HashSet<Collider2D>();

    public bool Colliding {
        get {
            return CollidingObject;
        }
    }

    public Collider2D CollidingObject {
        get {
            foreach (var collider in colliding) {
                if (collider)
                    return collider;
            }
            return null;
        }
    }

    public Collider2D[] CollidingObjects {
        get {
            var ret = new Collider2D[colliding.Count];
            colliding.CopyTo(ret);
            return ret;
        }
    }

    public System.Action<Collider2D> startedSensing, stoppedSensing;

    public void Clear() {
        colliding.Clear();
    }

    void Start() {
        filters = GetComponents<CollisionFilter>();
    }

    IEnumerator ActionCleanup() {
        var wait = new WaitForSeconds(0.5f);
        while (true) {
            yield return wait;
            
            colliding.RemoveWhere(it => !it);
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        foreach (var filter in filters) {
            if (!filter.Accepts(other)) {
                return;
            }
        }

        if (!colliding.Contains(other)) {
            colliding.Add(other);

            if (startedSensing != null) {
                startedSensing(other);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (colliding.Contains(other)) {
            colliding.Remove(other);
            
            if (stoppedSensing != null) {
                stoppedSensing(other);
            }
        }
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(Sensor))]
public class SensorInspector : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (Application.isPlaying) {
            var sensor = (Sensor) target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colliding: " + sensor.colliding.Count);

            foreach (var collider in sensor.colliding) {
                EditorGUILayout.ObjectField(collider, typeof(Sensor), allowSceneObjects: true);
            }

            Repaint();
        }
    }

}

#endif