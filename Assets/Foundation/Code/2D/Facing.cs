using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Facing {
    Left, Right
}

public static class ExtFacing {

    public static float GetDirection(this Facing facing) {
        return facing == Facing.Right ? 1 : -1;
    }

    public static Facing Reverse(this Facing facing) {
        return facing == Facing.Right ? Facing.Left : Facing.Right;
    }

}
