using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSineFloat : MonoBehaviour {

    public float amp;
    public float freq;

    public bool enableByRigidbodySpeed;

    private Vector3 _initPos;
    private Rigidbody _rb;

    private void Awake() {
        _initPos = transform.localPosition;
        _rb = GetComponentInParent<Rigidbody>();
    }

    private void Update() {
        bool doFloat = true;
        if (enableByRigidbodySpeed) {
            doFloat = _rb.velocity.sqrMagnitude > 1f;
        }

        if (doFloat)
            transform.localPosition = _initPos + Vector3.up * (amp * Mathf.Sin(Time.time * freq));
        else
            transform.localPosition = _initPos;
    }
}
