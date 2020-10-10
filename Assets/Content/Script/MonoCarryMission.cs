using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class MonoCarryMission : MonoListenEventBehaviour {

    private bool _started;

    public IntRange moneyAdd;

    public MonoInteractable startInteract;
    public MonoInteractable endInteract;
    public MonoMissionHint endMissionHint;

    private StudioEventEmitter _coinSound;

    private void Awake() {
        endInteract.enabled = false;
        startInteract.enabled = true;

        _coinSound = gameObject.AddComponent<StudioEventEmitter>();
        _coinSound.Event = "event:/coin";
    }

    protected override void OnDisable() {
        base.OnDisable();
        if (_started) {
            GameContext.Instance.playerCtl.CarryMission(false);
            EventBus.Post(new GeneralHintEvent { msg = "Mission canceled!" });
        }
        _started = false;
        endInteract.enabled = false;
        startInteract.enabled = true;
    }

    [SubscribeEvent]
    private void OnInteract(InteractEvent ev) {
        if (ev.interactable == startInteract) {
            if (!_started) {
                _started = true;
                endInteract.enabled = true;
                startInteract.enabled = false;
                EventBus.Post(new GeneralHintEvent { msg = "Go to the end post to finish the mission!" });
                GameContext.Instance.playerCtl.CarryMission(true);

                MissionManager.currentMission = endMissionHint;
            }
        } else if (ev.interactable == endInteract) {
            if (_started) {
                _started = false;
                endInteract.enabled = false;
                startInteract.enabled = true;

                GameContext.Instance.player.GetComponent<MonoPlayer>().money += moneyAdd.random;
                GameContext.Instance.playerCtl.CarryMission(false);
                _coinSound.Play();
                EventBus.Post(new GeneralHintEvent { msg = "Mission finished!" });
            }
        }
    }

}
