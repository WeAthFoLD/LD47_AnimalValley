using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    public static MainCamera instance { get; private set; }

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

    public Transform cameraSplitTarget { get; set; }

    private Vector2 _lastTargetPosition;

    Camera cam;

    Vector2 position;
    Vector2 velocity;

    float sizeDampVelocity;

    List<Shake> activeShakes = new List<Shake>();

    List<CameraAttractor> activeAttractors = new List<CameraAttractor>();

    List<CameraResizeArea> resizeAreas = new List<CameraResizeArea>();

    List<CameraSplit> splits = new List<CameraSplit>(); 

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

    public void AddAttractor(CameraAttractor attractor) {
        activeAttractors.Add(attractor);
    }

    public void RemoveAttractor(CameraAttractor attractor) {
        activeAttractors.Remove(attractor);
    }

    public void AddResizeArea(CameraResizeArea area) {
        resizeAreas.Add(area);
    }

    public void RemoveResizeArea(CameraResizeArea area) {
        resizeAreas.Remove(area);
    }

    public void AddCameraSplit(CameraSplit split) {
        splits.Add(split);
        splits.Sort((lhs, rhs) => -lhs.priority.CompareTo(rhs.priority));
    }

    public void RemoveCameraSplit(CameraSplit split) {
        splits.Remove(split);
    }

    void LateUpdate () {
        var targetPos = CalculateTargetPos();
        position = Vector2.SmoothDamp(position, targetPos, 
                                 ref velocity, smoothTime, maxSpeed, Time.deltaTime);

        Vector2 finalPos = position;
        
        foreach (var shake in activeShakes) {
            var progress = shake.elapsed / shake.duration;
            var offx = Mathf.PerlinNoise(shake.perlinOffset.x, shake.elapsed * shake.freq) - 0.5f;
            var offy = Mathf.PerlinNoise(shake.perlinOffset.y, shake.elapsed * shake.freq) - 0.5f;
            var damping = 1 - Mathf.SmoothStep(0, 1, progress);

            finalPos += new Vector2(offx, offy) * damping * shake.amp;

            shake.elapsed += Time.unscaledDeltaTime; // Ignore time scale change
        }

        activeShakes.RemoveAll(it => it.elapsed >= it.duration);
                                
        transform.position = MathUtil.PixelSnap(new Vector3(finalPos.x, finalPos.y, transform.position.z));

        UpdateCameraSize(targetPos);

        _lastTargetPosition = targetPos;
    }

    void UpdateCameraSize(Vector2 targetPos) {
        float resizeSum = 0;
        int resizeCount = 0;
        foreach (var area in resizeAreas) {
            float res = area.Resize(targetPos, initialSize);
            if (res != -1) {
                resizeSum += res;
                resizeCount++;
            }
        }

        float newSize;
        if (resizeCount == 0) {
            newSize = initialSize;
        } else {
            newSize = resizeSum / resizeCount;
        }

        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, newSize, ref sizeDampVelocity, 0.3f, 3.0f);
    }

    Vector2 CalculateTargetPos() {
        Vector2 targetPos = target ? (Vector2) target.position : _lastTargetPosition;

        Vector2 avgAttractorPos = Vector2.zero;
        float totalWeight = 0;

        foreach (var attractor in activeAttractors) {
            var weight = attractor.currentWeight;
            avgAttractorPos += weight * (Vector2) attractor.transform.position;
            totalWeight += weight;
        }

        if (totalWeight != 0) {
            avgAttractorPos /= totalWeight;
            targetPos = Vector2.Lerp(targetPos, avgAttractorPos, Mathf.Min(1, totalWeight));
        }

        Vector2 cameraSize = new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);

        foreach (var split in splits) {
            targetPos = split.TryLimit(targetPos, cameraSize);
        }

        return targetPos;
    }
    
}
