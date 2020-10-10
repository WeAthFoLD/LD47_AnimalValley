using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using SPlay;
using UnityEngine;

public class MonoInteractable : MonoBehaviour, ExpressionContext {

    public string interactHint;

    public string onInteract;

    [EventRef]
    public string havestSound = "event:/interact";

    private Expression _onInteractExpr;

    private StudioEventEmitter _harvestSound;

    private void Awake() {
        if (!string.IsNullOrEmpty(onInteract))
            _onInteractExpr = ExpressionParser.Parse(onInteract);

        _harvestSound = gameObject.AddComponent<StudioEventEmitter>();
        _harvestSound.Event = havestSound;
    }

    public virtual void OnInteract() {
        _onInteractExpr?.Evaluate(this);
        EventBus.Post(new InteractEvent { interactable = this });

        _harvestSound.Play();
    }

    public object GetVariable(StringHash nameHash) {
        if (nameHash == new StringHash("Owner")) {
            return gameObject;
        }

        return null;
    }
}
