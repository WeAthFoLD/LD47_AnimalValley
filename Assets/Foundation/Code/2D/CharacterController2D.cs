using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using URandom = UnityEngine.Random;

[RequireComponent (typeof (Rigidbody2D))]
// [RequireComponent (typeof (BoxCollider2D))]
public class CharacterController2D : MonoBehaviour {
    const float SkinWidth = 0.05f;
    const float Bias = 0.01f;
    const float SizeShrink = 1e-2f;

    public enum ManageMode {
        Velocity,
        Position,
        GroundVelocity
    }

    [System.Serializable]
    public struct State {
        public bool enableCollision;
        public bool enableGravity;
        public bool updateVelocity;
        public bool updateGrounded;
    }

    #region PublicField

    public float RayPrecision = 0.1f;

    public LayerMask layerMaskBase;

    public LayerMask layerMaskHoriz;

    public float gravity = 30.0f;

    public float maxFallingSpeed = 25;

    [InfoBox("Use RaycastAll to ensure the character doesn't go out of collider bounds. Require more calculation resource.")]
    public bool accurateCollision = false;

    public bool updateLastGroundedPos = false;

    public BoxCollider2D customCollider;

    public State state = new State {
        enableCollision = true,
        enableGravity = true,
        updateGrounded = true,
        updateVelocity = true
    };

    // Callback when player is grounded, parameter: previous falling speed
    public System.Action<float> onGrounded;

    public Func<Collider2D, bool> collisionFilter;

    public bool grounded {
        get {
            return onGround;
        }
        set {
            bool raiseEvent = onGround != value;
            onGround = value;
            if (raiseEvent && onGround)
                this.onGrounded(0);
        }
    }

    public float airborneTime {
        get;
        private set;
    }

    public List<RaycastHit2D> horizontalCollisions {
        get {
            return _horizontalCollisions;
        }
    }

    public List<RaycastHit2D> verticalCollisions {
        get {
            return _verticalCollisions;
        }
    }

    public Vector2 groundNormal {
        get {
            return _groundNormal;
        }
    }

    public readonly HashSet<Collider2D> touchingGrounds = new HashSet<Collider2D> ();

    // public MovingGround movingGround { get; private set; }

    [ShowInInspector, ReadOnly]
    public bool rightHasGround { get; private set; } // 右侧是否有地面
    [ShowInInspector, ReadOnly]
    public bool leftHasGround { get; private set; } // 左侧是否有地面

    public Vector2 lastGroundedPos { get; private set; }

    [NonSerialized]
    public Vector2 velocity;

    [NonSerialized]
    public Vector2 lastPosition, position;

    public BoxCollider2D boxCollider { get; private set; }
    #endregion


    float lastFixedUpdateTime;

    Vector2 _groundNormal = Vector2.up;

    Rigidbody2D rb;

    bool onGround;

    readonly List<RaycastHit2D> _horizontalCollisions = new List<RaycastHit2D> ();
    readonly List<RaycastHit2D> _verticalCollisions = new List<RaycastHit2D> ();

    Bounds bounds;


    float notGroundedTime, airLockTime;

    bool strictlyGrounded = false;

    public float curMaxFallingSpeed = 0;

    float groundvel;

    enum IgnoreState { Ignore, Recovering }

    const float IgnoreRecoverTime = 0.21f;

    Dictionary<Collider2D, IgnoreState> _ignoredColliders = new Dictionary<Collider2D, IgnoreState> ();

    readonly HashSet<Vector2> cachedPerFrameNormals = new HashSet<Vector2>();
    readonly List<RaycastHit2D> cachedPreFrameCollision = new List<RaycastHit2D>();

    private void Awake() {
        rb = GetComponent<Rigidbody2D> ();
    }

    void Start () {
        boxCollider = customCollider ? customCollider : GetComponent<BoxCollider2D> ();
        _probeUpdateCounter = URandom.Range(0, 100);

        lastPosition = position = rb.position;
    }

    public Vector2 MoveImmediate (Vector2 delta) {
        delta = Move (delta);
        rb.position = transform.position = position;
        return delta;
    }

    public Vector2 Move (Vector2 delta) {
        return MoveInternal (delta, false);
    }

    // It is required to call this function to reset the onGrounded flag forcibly when character jumps.
    public void OnJump () {
        this.notGroundedTime = 1.0f;
        this.strictlyGrounded = false;
        this.onGround = false;
        this.airLockTime = 0.2f;
    }

    Vector2 MoveInternal (Vector2 delta, bool updateVelocity) {
        _horizontalCollisions.Clear ();
        _verticalCollisions.Clear ();

        var originalDelta = delta;

        if (updateVelocity) {
            MoveHorizontally (ref delta, ref velocity, originalDelta);
            MoveVertically (ref delta, ref velocity, originalDelta);
        } else {
            Vector2 tmp = Vector2.zero;
            MoveHorizontally (ref delta, ref tmp, originalDelta);
            MoveVertically (ref delta, ref tmp, originalDelta);
        }

        position += delta;
        return delta;
    }

    public void Teleport (Vector2 npos, bool resetInterpolate = true) {
        if (resetInterpolate) {
            lastPosition = position = npos;
            rb.position = position;
            transform.position = lastPosition;
        } else {
            position = npos;
        }
        velocity = Vector2.zero;
        curMaxFallingSpeed = 0f;
    }

    public void TeleportMove (Vector2 delta, bool resetInterpolate = true) {
        Teleport ((Vector2) position + delta, resetInterpolate);
    }

    public void IgnoreCollider(Collider2D col) {
        _ignoredColliders.Add(col, IgnoreState.Ignore);
    }

    public void StopIgnoreCollider(Collider2D col) {
        _ignoredColliders.Remove(col);
    }

    bool InRect (Vector2 origin, Vector2 size, Vector2 point) {
        var delta = point - origin;
        return Mathf.Abs (delta.x) < size.x && Mathf.Abs (delta.y) < size.y;
    }

    private float unbiased (float biased) {
        if (Mathf.Abs (Mathf.Round (biased) - biased) < 0.001) {
            return Mathf.Round (biased);
        } else {
            return biased;
        }
    }

    void MoveHorizontally (ref Vector2 delta, ref Vector2 velocity, Vector2 originalDelta) {
        if (!state.enableCollision || !boxCollider.enabled)
            return;

        if (delta.x == 0)
            return;

        // Debug.DrawRay(bounds.max, Vector3.right * delta.x, Color.black);
        MoveHorizontallySub (ref delta, ref velocity, originalDelta);

        // Vector2 reversedDelta = Vector2.right * 1e-5f * (originalDelta.x > 0 ? -1 : 1);
        // Vector2 tempVelocity = Vector2.zero;
        // MoveHorizontallySub(ref reversedDelta, ref tempVelocity, reversedDelta);

        // delta += reversedDelta;
    }

    void MoveHorizontallySub (ref Vector2 delta, ref Vector2 velocity, Vector2 originalDelta) {
        int usedLayerMask = layerMaskBase | layerMaskHoriz;
        // int usedLayerMask = layerMaskBase;
        int horizontalRays = Mathf.CeilToInt (bounds.size.y / RayPrecision);

        bool moveRight = delta.x > 0;
        float y0 = bounds.min.y + SizeShrink;
        float step = (bounds.size.y - 2 * SizeShrink) / (horizontalRays - 1);
        float x0;
        Vector2 dir;
        if (moveRight) {
            x0 = bounds.min.x + SkinWidth;
            dir = Vector2.right;
        } else {
            x0 = bounds.max.x - SkinWidth;
            dir = Vector2.left;
        }

        float rayLength = Mathf.Abs (delta.x) + bounds.size.x - SkinWidth + Bias;

        RaycastHit2D finalHit = new RaycastHit2D ();
        float finalDeltaX = float.NaN;

        for (int i = 0; i < horizontalRays; ++i) {
            var ro = new Vector2 (x0, y0);
            var hit = accurateCollision ? RaycastAll(ro, dir, rayLength, usedLayerMask) : Raycast(ro, dir, rayLength, usedLayerMask);

            if (hit && 
                (!hit.collider.sharedMaterial || hit.collider.sharedMaterial.name != "Ground" || Mathf.Abs(hit.normal.x) < 0.8f) &&
                AcceptCollider (hit.collider) && 
                !hit.collider.OverlapPoint (ro) && 
                !CheckCollisionIgnore (hit.point, hit.normal)) {
                delta = RemoveProjection (delta, hit.normal);
                velocity = RemoveProjection (velocity, hit.normal);

                var slope = -hit.normal.y / hit.normal.x;
                var lineX = slope * (hit.point.y - bounds.min.y) + bounds.center.x;

                float alignX;
                if (moveRight) {
                    alignX = bounds.max.x;
                    if (hit.normal.x < 0 && slope != 0) {
                        alignX = Mathf.Min (bounds.max.x, lineX);
                    }
                } else {
                    alignX = bounds.min.x;
                    if (hit.normal.x > 0 && slope != 0) {
                        alignX = Mathf.Max (bounds.min.x, lineX);
                    }
                }

                var deltaX = hit.point.x - alignX;
                if (Mathf.Abs (deltaX) > bounds.size.x * 0.3f) {
                    deltaX = delta.x;
                }

                if (float.IsNaN (finalDeltaX) || (deltaX - finalDeltaX) * dir.x > 0) {
                    finalHit = hit;
                    finalDeltaX = deltaX;
                }

                _horizontalCollisions.Add (hit);
            }

            y0 += step;
        }

        if (finalHit) {
            if (finalHit.collider.IsTouching (boxCollider)) {
                finalHit.normal = -dir;
            }

            delta.x = finalDeltaX;
        }
    }

    void MoveVertically (ref Vector2 delta, ref Vector2 velocity, Vector2 originalDelta) {
        if (!state.enableCollision || !boxCollider.enabled)
            return;

        int usedLayerMask = layerMaskBase;
        int verticalRays = Mathf.CeilToInt (bounds.size.x / RayPrecision);

        float x0 = bounds.min.x + SizeShrink;
        float step = (bounds.size.x - 2 * SizeShrink) / (verticalRays - 1);
        float y0;
        Vector2 dir;
        bool moveUp = delta.y > 1.0e-2f;
        if (moveUp) {
            y0 = bounds.min.y + SkinWidth;
            dir = Vector2.up;
        } else {
            y0 = bounds.max.y - SkinWidth;
            dir = Vector2.down;
        }

        float rayLength = Mathf.Abs (delta.y) + bounds.size.y - SkinWidth + Bias;

        RaycastHit2D finalHit = new RaycastHit2D ();
        float finalDeltaY = float.NaN;

        List<RaycastHit2D> rawHits = cachedPreFrameCollision;
        HashSet<Vector2> normals = cachedPerFrameNormals;
        rawHits.Clear();
        normals.Clear();

        for (int i = 0; i < verticalRays; ++i) {
            var ro = new Vector2 (x0, y0);
            var hit = Raycast (ro, dir, rayLength, usedLayerMask);
            if (hit && !hit.collider.OverlapPoint (ro)) {
                rawHits.Add (hit);
                normals.Add (hit.normal);
            }
            x0 += step;
        }

        foreach (var hit in rawHits) {
            if (hit && AcceptCollider (hit.collider)) {
                Vector2 hitPoint = hit.point;
                // TODO: 这里GetComponent的GC很烦人（虽然是EditorOnly的），确实也有性能开销，最好弄个更好的方法
                // {
                //     var movingGround = hit.collider.GetComponent<MovingGround>();
                //     if (movingGround != null) {
                //         float factor = movingGround.movingVelocity.y <= 0 ? 1.0f : 0.0f;
                //         hitPoint += movingGround.movingVelocity * Time.deltaTime * factor;
                //     }
                // }

                if (!CheckCollisionIgnore2 (hitPoint, normals) && Vector2.Dot (dir, hit.normal) < 0) {
                    delta = RemoveProjection (delta, hit.normal);
                    velocity = RemoveProjection (velocity, hit.normal);

                    float alignY = moveUp ? bounds.max.y : bounds.min.y;
                    if (!moveUp) {
                        var slope = -hit.normal.x / hit.normal.y;
                        var lineY = slope * (hitPoint.x - bounds.center.x) + bounds.min.y;
                        alignY = Mathf.Max(bounds.min.y, lineY);
                    }

                    float deltaY = hitPoint.y - alignY;

                    if (Mathf.Abs (deltaY) > bounds.size.y * 0.3f) {
                        deltaY = delta.y;
                    }

                    if (float.IsNaN (finalDeltaY) || Mathf.Abs (finalDeltaY) > Mathf.Abs (deltaY)) {
                        finalHit = hit;
                        finalDeltaY = deltaY;
                    }

                    // Debug.DrawRay (hitPoint, hit.normal, Color.yellow, 0.3f);
                    _verticalCollisions.Add (finalHit);
                }
            }
        }

        if (finalHit) {
            if (finalHit.collider.IsTouching (boxCollider)) {
                finalHit.normal = -dir;
            }

            delta.y = finalDeltaY;
        }
    }

    bool AcceptCollider (Collider2D col) {
        return !_ignoredColliders.ContainsKey (col) && 
            (!col.sharedMaterial || col.sharedMaterial.name != "IgnoreCollision") &&
            (collisionFilter == null || collisionFilter(col));
    }

    bool CheckCollisionIgnore2 (Vector2 point, HashSet<Vector2> normals) {
        foreach (var n in normals) {
            if (CheckCollisionIgnore (point, n))
                return true;
        }
        return false;
    }

    bool CheckCollisionIgnore (Vector2 point, Vector2 normal) {
        if (normal.y <= 0 || normal.x == 0) return false;
        var slope = -normal.x / normal.y;
        var lineY = slope * (point.x - bounds.center.x) + bounds.min.y;
        return lineY > point.y + Bias;
        // return false;
    }

    void UpdateOnGround () {
        const float GroundProbeDistance = 0.3f;

        if (!state.enableCollision || !boxCollider.enabled)
            return;

        touchingGrounds.Clear ();

        int verticalRays = Mathf.CeilToInt (bounds.size.x / RayPrecision);

        float x0 = bounds.min.x + Bias;
        float step = (bounds.size.x - Bias * 2) / (verticalRays - 1);
        float y0 = bounds.max.y - SkinWidth;
        Vector2 dir = Vector2.down;

        float rayLength = bounds.size.y - SkinWidth + GroundProbeDistance;

        Vector2 groundNormalSum = Vector2.zero;
        bool hitGround = false;
        bool hitGroundWeak = hitGround;

        foreach (var hit in _verticalCollisions) {
            if (hit.normal.y > 0.5f) {
                hitGround = true;
                touchingGrounds.Add (hit.collider);
                break;
            }
        }

        // movingGround = null;
        for (int i = 0; i < verticalRays; ++i) {
            var ro = new Vector2 (x0, y0);
            var hit = Raycast (ro, dir, rayLength, layerMaskBase);
            if (Mathf.Abs (hit.normal.y) >= 0.3f && !hit.collider.OverlapPoint (ro) && AcceptCollider (hit.collider)) {
                // Debug.DrawRay (ro, hit.normal, Color.black);
                if (hit && AcceptCollider (hit.collider)) {
                    groundNormalSum += hit.normal;
                    hitGroundWeak = true;
                    touchingGrounds.Add (hit.collider);
                }

                var hitPoint = hit.point;
                // MovingGround mvg = hit.collider.GetComponent<MovingGround>();
                // if (mvg != null) {
                //     var dvel = mvg.movingVelocity;
                //     dvel = new Vector2(Mathf.Abs(dvel.x), Mathf.Abs(dvel.y));
                //     hitPoint += dvel * Time.deltaTime;
                //     movingGround = mvg;
                // }

                if (y0 - hit.point.y <= bounds.size.y - SkinWidth + Bias) {
                    hitGround = true;
                }
            }

            x0 += step;
        }

        if (groundNormalSum != Vector2.zero) {
            _groundNormal = groundNormalSum.normalized;
        }

        airLockTime -= Time.deltaTime;

        if (hitGround) {
            if (airLockTime <= 0) {
                notGroundedTime = 0;
                onGround = true;
            }
        } else {
            notGroundedTime += Time.deltaTime;
            if (notGroundedTime > 0.03 && !hitGroundWeak) {
                onGround = false;
            }
        }

        strictlyGrounded = hitGround;
    }

    private static readonly RaycastHit2D[] _RaycastResults = new RaycastHit2D[16];

    RaycastHit2D RaycastAll(Vector2 ro, Vector2 dir, float rayLength, int layerMask) {
        var size = Physics2D.RaycastNonAlloc(ro, dir, _RaycastResults, rayLength, layerMask);
        RaycastHit2D ret = new RaycastHit2D();
        float distSq = float.MaxValue;
        for (int i = 0; i < size; ++i) {
            var result = _RaycastResults[i];
            if (AcceptCollider(result.collider)) {
                float dist = (result.point - ro).sqrMagnitude;
                if (dist < distSq) {
                    ret = result;
                }
            }
        }
        return ret;
    }

    RaycastHit2D Raycast (Vector2 ro, Vector2 dir, float rayLength, int layerMask) {
        return Physics2D.Raycast(ro, dir, rayLength, layerMask);
    }

    void FixedUpdate () {
        var dt = Time.deltaTime;

        if (boxCollider.enabled && ((Vector2) transform.position - position).sqrMagnitude > boxCollider.size.x * 2.0f) {
            lastPosition = position = transform.position;
        } else {
            lastPosition = position;
        }

        // Recalculate private bounds based on internal position
        {
            var colliderCenter = boxCollider.offset;
            var colliderSize = boxCollider.size;
            bounds = new Bounds (position + colliderCenter, colliderSize);
        }

        // On Ground update
        if (state.updateGrounded) {
            var prevGrounded = grounded;
            UpdateOnGround ();

            if (!grounded) {
                curMaxFallingSpeed = Mathf.Max (curMaxFallingSpeed, -velocity.y);
            } 

            if (!prevGrounded && grounded) {
                if (this.onGrounded != null)
                    onGrounded (curMaxFallingSpeed);
                curMaxFallingSpeed = 0;
                airborneTime = 0;
            }
        } else {
            // movingGround = null;
        }

        if (!grounded) {
            airborneTime += dt;
        }
        //

        // Gravity
        if (state.enableGravity && !strictlyGrounded) {
            velocity.y -= gravity * dt;
        }

        if (velocity.y < -maxFallingSpeed) {
            velocity.y = -maxFallingSpeed;
        }

        // Move
        if (state.updateVelocity) {
            MoveInternal (velocity * dt, true);
        } else {
            //velocity = Vector2.zero;
        }

        // // Move if on moving platform
        // if (movingGround != null) {
        //     MoveInternal(movingGround.movingVelocity * dt, false);
        // }

        rb.position = position;

        ++_probeUpdateCounter;
        UpdateLastGroundedPos();
        UpdateSideHasGround();
    }

    void Update () {
        transform.position = Vector3.Lerp (lastPosition, position, (Time.time - Time.fixedTime) / Time.fixedDeltaTime);
    }

    private int _probeUpdateCounter;

    void UpdateLastGroundedPos() {
        if (updateLastGroundedPos && _probeUpdateCounter % 5 == 0 && touchingGrounds.Count > 0) {
            const float SafeOffset = 0.5f;
            const float RayOffDelta = 0.1f;
            var bounds = boxCollider.bounds;
            var midY = bounds.center.y;
            var leftX = bounds.min.x;
            var rightX = bounds.max.x;

            var rightPos = new Vector2(rightX + SafeOffset, midY);
            var leftPos = new Vector2(leftX - SafeOffset, midY);
            var layerMask = layerMaskBase;
            var rayLength = bounds.extents.y + RayOffDelta;

            if (Physics2D.Raycast(leftPos, Vector2.down, rayLength, layerMask) &&
                Physics2D.Raycast(rightPos, Vector2.down, rayLength, layerMask)) {
                lastGroundedPos = position;
            }
        }
    }

    void UpdateSideHasGround() {
        if (_probeUpdateCounter % 3 == 0) {
            const float XDelta = 0.05f;
            const float YDelta = 0.1f;
            var bounds = boxCollider.bounds;
            var rayOrigin = new Vector2(bounds.max.x + XDelta, bounds.center.y);
            var rayOrigin2 = new Vector2(bounds.min.x - XDelta, bounds.center.y);
            var rayLength = bounds.extents.y + YDelta;

            rightHasGround = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, layerMaskBase);
            leftHasGround = Physics2D.Raycast(rayOrigin2, Vector2.down, rayLength, layerMaskBase);
        }
    }

    void OnDrawGizmos () {
        Gizmos.color = Color.red;
        Gizmos.DrawRay (transform.position, groundNormal);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay (transform.position, velocity / 5.0f);
    }

    Vector2 RemoveProjection (Vector2 vec, Vector2 dir) {
        return vec - Vector2.Dot (vec, dir) * dir;
    }

}