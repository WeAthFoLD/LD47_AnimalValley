using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class MonoBGMDistsortion : MonoBehaviour {
    private StudioEventEmitter _emitter;

    private void Awake() {
        _emitter = GetComponent<StudioEventEmitter>();
        _emitter.SetParameter("AudioDistortion", 0f);
    }

    void Update() {
        if (!GameContext.Instance || !GameContext.Instance.playerCtl)
            return;

        var hunger = GameContext.Instance.playerCtl.hungerModule.currentHunger;
        float distort;
        if (hunger > 1f)
            distort = 0f;
        else
            distort = 1 - (hunger / 1f);

        _emitter.SetParameter("AudioDistortion", distort);
    }
}