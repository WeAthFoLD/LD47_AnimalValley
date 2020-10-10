using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionFilterMaterials : CollisionFilter {

	public List<PhysicsMaterial2D> materials = new List<PhysicsMaterial2D>();

	public bool negate;
	
    public override bool Accepts(Collider2D collider) {
		return (collider.sharedMaterial != null && materials.Contains(collider.sharedMaterial)) != negate;
    }
}
