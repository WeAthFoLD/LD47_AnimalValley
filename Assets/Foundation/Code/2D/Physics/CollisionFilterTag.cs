using UnityEngine;
using System.Collections.Generic;

public class CollisionFilterTag : CollisionFilter {

    public List<string> tags;
    public bool useRigidbodyTag = true;

    public override bool Accepts(Collider2D collider) {
        if (tags == null)
            return false;

        GameObject go = collider.gameObject;
        if (useRigidbodyTag && collider.attachedRigidbody) {
            go = collider.attachedRigidbody.gameObject;
        }
        return tags.Contains(go.tag);
    }

}