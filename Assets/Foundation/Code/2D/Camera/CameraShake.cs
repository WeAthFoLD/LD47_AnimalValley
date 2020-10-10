using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour {

    float XShakeExtent = 1.0f;

    float YShakeExtent = 2.0f;

    float ShakeTime = 0.5f;

    Vector2 offsetRecord = new Vector2 (0, 0);

    bool shaking;

    Transform camera_;

    bool decreas;

    float shakedTime = 0;

    Vector2 targetPoint = Vector2.zero;

    void Start () {
        
    }

    void LateUpdate () {
        float cameraDelta = 0.2f;
        if (!camera_)
            if (Camera.main)
                camera_ = Camera.main.transform;
        if (camera_) {
            if (shaking) {
                if (shakedTime < ShakeTime - 0.2f) {
                    if ((offsetRecord - targetPoint).magnitude > 0.05) {
                        float x = Mathf.MoveTowards (offsetRecord.x, targetPoint.x, cameraDelta) - offsetRecord.x;
                        float y = Mathf.MoveTowards (offsetRecord.y, targetPoint.y, cameraDelta) - offsetRecord.y;
                        offsetRecord += new Vector2 (x, y);
                        camera_.localPosition += new Vector3 (x, y);
                    } else {
                        targetPoint = new Vector2 (-(XShakeExtent / 2) + Random.Range (0, XShakeExtent), -(YShakeExtent / 2) + Random.Range (0, YShakeExtent)) - offsetRecord;
                        offsetRecord = Vector2.zero;
                    }
                } else {
                    float x = Mathf.MoveTowards (offsetRecord.x, 0, cameraDelta) - offsetRecord.x;
                    float y = Mathf.MoveTowards (offsetRecord.y, 0, cameraDelta) - offsetRecord.y;
                    offsetRecord += new Vector2 (x, y);
                    camera_.localPosition += new Vector3 (x, y);
                    if (offsetRecord.magnitude < 0.01) {
                        shaking = false;
                    }
                }
                shakedTime += Time.deltaTime;
            }
        }
    }

    public void Shake (float time, float x, float y) {
        if (!shaking) {
            this.ShakeTime = time;
            this.XShakeExtent = x;
            this.YShakeExtent = y;
            this.shaking = true;
            this.shakedTime = 0;
            this.offsetRecord = Vector2.zero;
        }
    }

}