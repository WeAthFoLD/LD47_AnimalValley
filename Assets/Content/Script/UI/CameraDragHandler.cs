using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraDragHandler : MonoListenEventBehaviour, IDragHandler {

    public float xRatio, yRatio;

    private MonoMainCamera _mainCamera;

    public void OnDrag(PointerEventData eventData) {
        float deltaYaw = eventData.delta.x * xRatio + eventData.delta.y * yRatio;
        _mainCamera.yaw += deltaYaw;
    }

    [SubscribeEvent]
    void OnPostInit(PostGameInitEvent ev) {
        _mainCamera = GameContext.Instance.mainCamera;
    }

}
