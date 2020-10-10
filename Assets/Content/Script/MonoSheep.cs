using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonoSheep : MonoBehaviour {

    public FloatRange moveSpeed;
    public FloatRange moveTime;
    public FloatRange idleTime;

    private Rigidbody _rb;
    [ShowInInspector, HideInEditorMode]
    private bool _moving;
    [ShowInInspector, HideInEditorMode]
    private float _timeRemain;

    private Vector3 _velocity;

    void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate() {
        var nvel = _rb.velocity;
        nvel.x = _velocity.x;
        nvel.z = _velocity.z;
        _rb.velocity = nvel;
    }

    void Update() {
        if (GameContext.Instance.pollutionState > PollutionState.None) {
            _velocity = Vector3.zero;
            return;
        }

        if (_timeRemain <= 0f) {
            _moving = !_moving;

            if (_moving) {
                _timeRemain = moveTime.random;
                var dir = Random.insideUnitCircle;
                _velocity = moveSpeed.random * new Vector3(dir.x, 0, dir.y);
            } else {
                _timeRemain = idleTime.random;
                _velocity = Vector3.zero;
            }
        }

        _timeRemain -= Time.deltaTime;
    }
}
