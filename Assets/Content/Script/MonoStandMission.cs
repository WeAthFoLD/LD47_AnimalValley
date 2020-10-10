
using System;
using FMODUnity;
using UnityEngine;

public class MonoStandMission : MonoListenEventBehaviour {

    public const string CTRL_KEY = "StandMission";

    public float duration;
    public IntRange addMoney;
    public MonoInteractable interactable;

    private StudioEventEmitter _coinSound;

    private bool _started = false;

    private float _remain = 0f;

    private void Awake() {
        _coinSound = gameObject.AddComponent<StudioEventEmitter>();
        _coinSound.Event = "event:/coin";
    }

    public void StartMission() {
        _started = true;
        var p = GameContext.Instance.player.GetComponent<MonoPlayer>();
        p.transform.position = transform.position;
        p.StandMissionStart();

        _remain = duration;
    }

    protected override void OnDisable() {
        base.OnDisable();

        if (_started) {
            var p = GameContext.Instance.playerCtl;
            p.disableControlCount.Remove(CTRL_KEY);
            _started = false;
            EventBus.Post(new GeneralHintEvent { msg = "Mission canceled!" });
        }
    }

    public void Update() {
        if (_started) {
            float lastRemain = _remain;
            _remain = Mathf.Max(0f, _remain - Time.deltaTime);
            if (Mathf.RoundToInt(lastRemain) != Mathf.RoundToInt(_remain)) {
                SendMSg();
            }

            if (_remain <= 0f) {
                _started = false;
                var p = GameContext.Instance.player.GetComponent<MonoPlayer>();
                p.disableControlCount.Remove(CTRL_KEY);

                p.money += addMoney.random;
                _coinSound.Play();
            }
        }
    }

    private void SendMSg() {
        EventBus.Post(new GeneralHintEvent {
            msg = $"Remain: 00:{Mathf.RoundToInt(_remain):D2}",
            overrideDuration = 0.99f,
            forceOverride = true
        });
    }

    [SubscribeEvent]
    private void OnInteract(InteractEvent ev) {
        if (!_started && ev.interactable == interactable) {
            StartMission();
        }
    }

}
