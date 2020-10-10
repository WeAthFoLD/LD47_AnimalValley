using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonoStageIconSwap : MonoListenEventBehaviour {
    private const bool InitAtGame = true;
    public Sprite overworldIcon, gameIcon;

    private Image _image;
    private SpriteRenderer sr;

    protected override void OnEnable() {
        base.OnEnable();
        _image = GetComponent<Image>();
        sr = GetComponent<SpriteRenderer>();

        var currentStage = StageManger.Instance
            ? StageManger.Instance.currentStage
            : (InitAtGame ? StageName.VirtualGame : StageName.Exterior);

        var icon = currentStage == StageName.VirtualGame ? gameIcon : overworldIcon;
        if (_image)
            _image.sprite = icon;
        if (sr)
            sr.sprite = icon;
    }

    [SubscribeEvent]
    private void _OnStageChange(SwapStageEvent evt) {
        var s = evt.newStage == StageName.Exterior ? overworldIcon : gameIcon;
        if (_image)
            _image.sprite = s;
        if (sr)
            sr.sprite = s;
    }
}
