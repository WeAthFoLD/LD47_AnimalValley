
using UnityEngine;

public static class GameObjectUtils {

    public static T GetCmpt<T>(this GameObject go) {
        return go.GetComponent<T>();
    }

    public static T GetCmpt<T>(this Component com) {
        return com.GetComponent<T>();
    }

    public static T GetCmpt<T>(this GameObject go, string path) {
        return go.transform.GetCmpt<T>(path);
    }

    public static T GetCmpt<T>(this Component cmpt, string path) {
        var child = cmpt.transform.Find(path);
        if (!child)
            return default;
        return child.GetComponent<T>();
    }

    public static void LazySetEnable(this MonoBehaviour cmpt, bool enable) {
        if (cmpt.enabled != enable)
            cmpt.enabled = enable;
    }

    public static void LasySetActive(this GameObject go, bool active) {
        go.SetActive(active);
    }

}
