using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;

public class MonoUIClickSound : MonoBehaviour, IPointerClickHandler {
    private StudioEventEmitter _emitter;

    private void Awake() {
        _emitter = gameObject.AddComponent<StudioEventEmitter>();
        _emitter.Event = "event:/interact";
    }

    public void OnPointerClick(PointerEventData eventData) {
        _emitter.Play();
    }
}
