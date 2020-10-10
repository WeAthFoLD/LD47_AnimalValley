using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonoPerlinShaker : MonoBehaviour {
    [Header("shake的目标，不填代表自己")]
    public Transform target;

    public float xRange;
    public float yRange;
    public float freq;
    public int octave = 1;

    private float _offsetX, _offsetY;
    private Vector2 _initLocalPos;
    private float _elapsed;

    void Awake() {
        if (!target)
            target = transform;

        _offsetX = Random.Range(0, 10000f);
        _offsetY = Random.Range(0, 10000f);
        _initLocalPos = target.localPosition;
    }

    private void OnDisable() {
        target.localPosition = _initLocalPos;
    }

    void Update() {
        _elapsed += Time.deltaTime;
        var shake = MathUtil.Shake(freq, new Vector2(xRange, yRange),
            new Vector2(_offsetX, _offsetY), octave, _elapsed);

        target.localPosition = _initLocalPos + shake;
    }
}