
using System;
using UnityEngine;
using UnityEngine.UI;

public class MonoInteractHintUI : MonoListenEventBehaviour {
    public GameObject contentRoot;
    public Text hintText;

    private void Awake() {
        contentRoot.SetActive(false);
    }

    [SubscribeEvent]
    void _OnTargetChanged(InteractableChangedEvent evt) {
        var player = GameContext.Instance.player.GetComponent<MonoPlayer>();
        var interactable = player.currentInteractable;

        contentRoot.SetActive(interactable);
        if (interactable) {
            hintText.text = interactable.interactHint;
        }
    }
}
