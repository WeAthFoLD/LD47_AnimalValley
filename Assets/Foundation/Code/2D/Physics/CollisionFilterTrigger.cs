using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionFilterTrigger : CollisionFilter {
    public bool acceptTrigger = true;
    public bool acceptNonTrigger = true;

    public override bool Accepts(Collider2D collider) {
        return collider.isTrigger ? acceptTrigger : acceptNonTrigger;
    }
}
