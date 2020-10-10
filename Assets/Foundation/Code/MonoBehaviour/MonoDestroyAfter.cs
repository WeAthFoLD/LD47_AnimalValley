using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoDestroyAfter : MonoBehaviour {
    public float duration;

    private float _elapsed;

    void Update() {
        _elapsed += Time.deltaTime;
        if (_elapsed > duration)
            Destroy(gameObject);
    }
}
