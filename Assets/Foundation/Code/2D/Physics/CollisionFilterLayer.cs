using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionFilterLayer : CollisionFilter {

    public LayerMask layerMask;

    public override bool Accepts(Collider2D collider) {
        return (layerMask.value & (1 << collider.gameObject.layer)) != 0;
    }

}
