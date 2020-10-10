using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FloatRange {
    public float from, to;

    public float length {
        get {
            return to - from;
        }
    }

    public float random {
        get {
            return from + Random.value * length;
        }
    }

    public FloatRange(float from, float to) {
        this.from = from;
        this.to = to;
    }

    public bool Contains(float pos) {
        return from <= pos && pos <= to;
    }
}