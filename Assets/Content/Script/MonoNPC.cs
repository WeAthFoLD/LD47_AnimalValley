using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoNPC : MonoBehaviour {

    public Transform route;
    public SpriteRenderer sr;

    public Sprite[] randomSprites;

    public float speed;

    private List<Transform> nodeList;

    private int _index;
    private float _walkDist;

    private void Awake() {
        if (route) {
            var children = new List<Transform>();
            foreach (Transform child in route) {
                children.Add(child);
            }

            nodeList = children;
        }

        sr.sprite = randomSprites[UnityEngine.Random.Range(0, randomSprites.Length)];
    }

    private void Update() {
        if (nodeList != null) {
            var cur = nodeList[_index];
            var next = nodeList[(_index + 1) % nodeList.Count];

            _walkDist += speed * Time.deltaTime;

            var dst = Vector3.Distance(cur.position, next.position);
            var ratio = _walkDist / dst;

            transform.position = Vector3.Lerp(cur.position, next.position, ratio);
            if (ratio >= 1f) {
                _index = (_index + 1) % nodeList.Count;
                _walkDist = 0f;
            }
        }
    }

    private void OnDrawGizmosSelected() {
        if (!route)
            return;

        Gizmos.color = Color.blue;
        for (int i = 0; i < route.childCount; ++i) {
            var cur = route.GetChild(i).position + Vector3.up;
            var next = route.GetChild((i + 1) % route.childCount).position + Vector3.up;
            Gizmos.DrawLine(cur, next);
        }
    }
}
