using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionFilterMaterial : CollisionFilter {

	public PhysicsMaterial2D material;
	public bool negate = false;

    public override bool Accepts(Collider2D collider) {
		return negate != (collider.sharedMaterial == material);
    }

}
