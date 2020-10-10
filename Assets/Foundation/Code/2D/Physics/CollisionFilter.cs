using UnityEngine;

public abstract class CollisionFilter : MonoBehaviour {

    public abstract bool Accepts(Collider2D collider);

}