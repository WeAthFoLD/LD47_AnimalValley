using UnityEngine;

public static class LayerMaskUtil {

    public static LayerMask GetLayerMask(params int[] layers) {
        LayerMask ret = 0;
        foreach (var layer in layers)
            ret |= 1 << layer;
        return ret;
    }

    public static bool Contains(this LayerMask layerMask, int layer) {
        return (layerMask & (1 << layer)) != 0;
    }

}
