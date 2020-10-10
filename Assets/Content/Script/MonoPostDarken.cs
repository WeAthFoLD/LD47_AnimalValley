using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class MonoPostDarken : MonoBehaviour {
    private PostProcessVolume _pv;

    private void Awake() {
        _pv = GetComponent<PostProcessVolume>();
    }

    void Update() {
        float strength = 0f;
        if (GameContext.Instance && GameContext.Instance.playerCtl) {
            var hunger = GameContext.Instance.playerCtl.hungerModule.currentHunger;
            if (hunger < 2) {
                strength = 1.0f - (hunger * 0.5f);
            }
        }

        _pv.weight = strength;
    }
}
