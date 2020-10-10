using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoMissionHint : MonoBehaviour {

    public MonoInteractable interactable;
    public SpriteRenderer sr;

    public string missionName;

    private void Awake() {
        MissionManager.Subscribe(this);
    }

    private void Update() {
        var shouldEnable = interactable.enabled;
        if (sr.enabled != shouldEnable)
            sr.enabled = shouldEnable;
    }
}
