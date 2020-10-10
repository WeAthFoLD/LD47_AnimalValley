using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoDrawGizmos : MonoBehaviour {
    // TODO: 按需添加其他类型

    public Color color;
    public float size;

    private void OnDrawGizmos() {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, size);
    }
}
