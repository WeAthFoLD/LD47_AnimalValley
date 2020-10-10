using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SortChildSprites {
    public enum SortType { Ascend, Descend }

    public static void Sort(Transform parentObject, bool sortXFirst, SortType sortX, SortType sortY, int initialDepth) {
        #if UNITY_EDITOR
        List<SpriteRenderer> renderers = new List<SpriteRenderer> (
            parentObject.GetComponentsInChildren<SpriteRenderer>()
        );

        renderers.Sort((lhs, rhs) => {
            Vector2 lpos = lhs.transform.position;
            Vector2 rpos = rhs.transform.position;
            int cmpX = Cmp(lpos.x, rpos.x, sortX);
            int cmpY = Cmp(lpos.y, rpos.y, sortY);

            if (sortXFirst) {
                return cmpX == 0 ? cmpY : cmpX;
            } else {
                return cmpY == 0 ? cmpX : cmpY;
            }
        });

        for (int i = 0; i < renderers.Count; ++i) {
            Undo.RecordObject(renderers[i], "Sort child sprites");
            renderers[i].sortingOrder = initialDepth + i;
        }
        #endif
    }

    static int Cmp(float l, float r, SortType s) {
        if (s == SortType.Ascend) {
            return l.CompareTo(r);
        } else { // Descend
            return r.CompareTo(l);
        }
    }

}
