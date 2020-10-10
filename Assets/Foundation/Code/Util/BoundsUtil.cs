using UnityEngine;

public static class BoundsUtils {

    public static Vector3 LeftTopPoint(this Bounds bounds) {
        var mn = bounds.min;
        var mx = bounds.max;
        return new Vector3(mn.x, mx.y, 0);
    }

    public static Vector3 RightTopPoint(this Bounds bounds) {
        var mx = bounds.max;
        return new Vector3(mx.x, mx.y, 0);
    }

    public static Vector3 LeftBottomPoint(this Bounds bounds) {
        var mn = bounds.min;
        var mx = bounds.max;
        return new Vector3(mn.x, mn.y, 0);
    }

    public static Vector3 RightBottomPoint(this Bounds bounds) {
        var mn = bounds.min;
        var mx = bounds.max;
        return new Vector3(mx.x, mn.y, 0);
    }

}
