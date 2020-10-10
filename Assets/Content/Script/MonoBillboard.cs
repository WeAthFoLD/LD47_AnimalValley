using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoBillboard : MonoBehaviour {

    void LateUpdate() {
        if (!GameContext.Instance)
            return;
        var mainCamera = GameContext.Instance.mainCamera;
        if (!mainCamera)
            return;

        var deltaPos = mainCamera.transform.position - transform.position;
        deltaPos.y = 0f;
        transform.forward = -deltaPos.normalized;
        // if (fullRotate) {
        //     transform.forward = deltaPos.normalized;
        // } else {
        //     var rotX = Mathf.Atan2(deltaPos.y, Mathf.Sqrt(deltaPos.x * deltaPos.x + deltaPos.z * deltaPos.z));
        //     transform.rotation = Quaternion.Euler(rotX * Mathf.Rad2Deg - AngleOffset, 0f, 0f);
        // }
    }
}
