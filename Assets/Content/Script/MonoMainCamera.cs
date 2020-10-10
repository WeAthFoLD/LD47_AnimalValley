
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MonoMainCamera : MonoListenEventBehaviour {

    public static MonoMainCamera instance { get; private set; }

    public class Shake {
        public float amp;
        public float freq;
        public float duration;

        public float elapsed;

        public Vector2 perlinOffset = new Vector2(Random.Range(0, 10000f), Random.Range(0, 1000f));
    }

	public float smoothTime, maxSpeed;

    [ReadOnly, ShowInInspector]
    public Transform target { get; set; }

    public Vector3 cameraOffset {
        get {
            var rad = Mathf.Deg2Rad * yaw;
            return new Vector3(
                Mathf.Cos(rad) * offsetWidth,
                offsetHeight,
                Mathf.Sin(rad) * offsetWidth
            );
        }
    }
    public float offsetHeight, offsetWidth;
    public float yaw = 0f;

    private Vector3 _lastTargetPosition;

    Camera cam;

    Vector3 position;
    Vector3 velocity;

    float sizeDampVelocity;

    List<Shake> activeShakes = new List<Shake>();

    [ShowInInspector, HideInEditorMode]
    float initialSize;

    void Start () {
        position = transform.position;
        cam = GetComponent<Camera>();
        initialSize = cam.orthographicSize;

        instance = this;
    }

    void OnEnable() {
        EventBus.Attach(this);
    }

    void OnDisable() {
        EventBus.Detach(this);
    }

    [SubscribeEvent]
    void OnStartShake(CameraShakeEvent evt) {
        float amp = evt.amp, freq = evt.freq, duration = evt.duration;
        int octaves = evt.octaves;
        while (octaves > 0) {
            activeShakes.Add(new Shake {
                amp = amp,
                freq = freq,
                duration = duration,
                elapsed = 0,
            });

            amp /= 2;
            freq *= 2;
            octaves -= 1;
        }
    }

    [SubscribeEvent]
    void OnTargetChange(CameraTargetChangeEvent evt) {
        target = evt.target;
        if (evt.resetPosition) {
            position = target.position;
            velocity = Vector2.zero;
            sizeDampVelocity = 0;
        }
    }

    [SubscribeEvent]
    void OnPosReset(CameraPositionResetEvent evt) {
        position = CalculateTargetPos();
        velocity = Vector2.zero;
    }

    void LateUpdate () {
        var targetPos = CalculateTargetPos();
        var cameraPos = targetPos;

        position = Vector3.SmoothDamp(position, cameraPos,
                                 ref velocity, smoothTime, maxSpeed, Time.deltaTime);

        Vector3 finalPos = position + cameraOffset;

        foreach (var shake in activeShakes) {
            var progress = shake.elapsed / shake.duration;
            var offx = Mathf.PerlinNoise(shake.perlinOffset.x, shake.elapsed * shake.freq) - 0.5f;
            var offy = Mathf.PerlinNoise(shake.perlinOffset.y, shake.elapsed * shake.freq) - 0.5f;
            var damping = 1 - Mathf.SmoothStep(0, 1, progress);

            finalPos += new Vector3(offx, offy) * damping * shake.amp;

            shake.elapsed += Time.unscaledDeltaTime; // Ignore time scale change
        }

        activeShakes.RemoveAll(it => it.elapsed >= it.duration);

        transform.position = new Vector3(finalPos.x, finalPos.y, finalPos.z);

        UpdateCameraSize(cameraPos);

        _lastTargetPosition = targetPos;

        var co = cameraOffset;
        transform.forward = -co;
    }

    void UpdateCameraSize(Vector2 targetPos) {
        float newSize = initialSize;
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, newSize, ref sizeDampVelocity, 0.3f, 3.0f);
    }

    Vector3 CalculateTargetPos() {
        Vector3 targetPos = target ? target.position : _lastTargetPosition;
        return targetPos;
    }

}
