using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAttractor : MonoBehaviour {
    public float innerRadius, outerRadius;
    public float weight;


    public float currentWeight {
        get {
            if (!player) return 0;

            var dist = (transform.position - player.position).magnitude;
            return weight * Mathf.SmoothStep(0, 1, 1 - (dist - innerRadius) / (outerRadius - innerRadius));
        }
    }


    MainCamera mainCamera {
        get {
            return MainCamera.instance;
        }
    }

    CircleCollider2D outerCollider;

    Transform player;

    void Start () {
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
        rb.freezeRotation = true;

        outerCollider = gameObject.AddComponent<CircleCollider2D>();
        outerCollider.radius = outerRadius;   
    }

    void OnCollisionEnter2D(Collision2D col) {
        if (AcceptsCollision(col)) {
            mainCamera.AddAttractor(this);
            player = col.collider.transform;
        }
    }

    void OnCollisionExit2D(Collision2D col) {
        if (AcceptsCollision(col))
            mainCamera.RemoveAttractor(this);
    }

    bool AcceptsCollision(Collision2D col) {
        return col.gameObject.tag == "Player";
    }

    void OnDisable() {
        if (mainCamera)
            mainCamera.RemoveAttractor(this);
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, innerRadius);

        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, outerRadius);
    }

}
