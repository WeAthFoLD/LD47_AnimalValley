using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSpriteBlend : MonoBehaviour {
    public SpriteRenderer sr;
    public float sourceAlpha;
    public float targetAlpha;
    public float duration;

    private Color _initColor;
    private float _elapsed;

    void Start() {
        _initColor = sr.color;
        Update();
    }

    void Update() {
        var t = Mathf.Min(_elapsed / duration);
        var alpha = Mathf.Lerp(sourceAlpha, targetAlpha, t);
        var newColor = _initColor;
        newColor.a = alpha;
        sr.color = newColor;

        _elapsed += Time.deltaTime;
    }
}
