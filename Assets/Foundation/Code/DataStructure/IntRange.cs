
using UnityEngine;

[System.Serializable]
public struct IntRange {
    public int from, to;

    public IntRange(int from, int to) {
        this.from = from;
        this.to = to;
    }

    public int random {
        get {
            return from + (int) ((to - from) * Random.value);
        }
    }

    public bool Contains(int val) {
        return from <= val && val <= to;
    }

    public bool ContainsExclusive(int val) {
        return from < val && val < to;
    }

}