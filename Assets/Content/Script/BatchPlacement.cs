using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BatchPlacement : MonoBehaviour {

    public GameObject[] prefabList;
    public float radius;
    public float floating;
    public float freq;
    public FloatRange deltaAngle;
    public FloatRange scaleRandom;

    #if UNITY_EDITOR
    [Button]
    void Place() {
        foreach (Transform t in transform) {
            DestroyImmediate(t.gameObject);
        }

        float angle = .0f;
        float rand = Random.Range(0, 10000);
        for (; angle < 360.0f; angle += deltaAngle.random) {
            var prefab = prefabList[Random.Range(0, prefabList.Length)];
            var actualRadius = radius + floating * Mathf.PerlinNoise(rand, freq * angle);

            var angleRad = angle * Mathf.Deg2Rad;
            var dpos = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * actualRadius;

            var instance = (GameObject) UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
            instance.transform.localPosition = dpos;
            instance.transform.localRotation = Quaternion.Euler(-180f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one * scaleRandom.random;
        }
    }
    #endif
}
