using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonoAccompanyMission : MonoListenEventBehaviour {

    // enum State {
    //     Idle, InMission, CD
    // }
    //
    // private State _state;
    public float cd;
    public float accompanyTime;
    public float distanceThresh;
    public float loseTimeThresh;
    public int canBeginMoneyThresh;
    public IntRange moneyBonus;
    public IntRange moneyPenalty;

    public FloatRange moveSpeed;
    public FloatRange moveTime;
    public FloatRange idleTime;

    private Rigidbody _rb;

    [ShowInInspector, HideInEditorMode]
    private bool _missionStarted;
    [ShowInInspector, HideInEditorMode]
    private bool _inCD;
    [ShowInInspector, HideInEditorMode]
    private bool _moving;
    [ShowInInspector, HideInEditorMode]
    private float _moveTimeRemain;

    private float _outOfRangeTimer;

    private float _accompanyTimer;

    private float _cdTimer;

    private Vector3 _velocity;

    private MonoInteractable _interact;

    private StudioEventEmitter _coinSound;

    void Awake() {
        _rb = GetComponent<Rigidbody>();
        _interact = GetComponent<MonoInteractable>();
        _coinSound = gameObject.AddComponent<StudioEventEmitter>();
        _coinSound.Event = "event:/coin";
    }

    private void FixedUpdate() {
        var nvel = _rb.velocity;
        nvel.x = _velocity.x;
        nvel.z = _velocity.z;
        _rb.velocity = nvel;
    }

    void Update() {

        if (_missionStarted) {
            if (_moveTimeRemain <= 0f) {
                _moving = !_moving;

                if (_moving) {
                    _moveTimeRemain = moveTime.random;
                    var dir = Random.insideUnitCircle;
                    _velocity = moveSpeed.random * new Vector3(dir.x, 0, dir.y);
                } else {
                    _moveTimeRemain = idleTime.random;
                    _velocity = Vector3.zero;
                }
            }

            _moveTimeRemain -= Time.deltaTime;

            var player = GameContext.Instance.playerCtl;
            var dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > distanceThresh) {
                _outOfRangeTimer += Time.deltaTime;
                EventBus.Post(new GeneralHintEvent {
                    msg = "<color=red>You are too far away from Jessica!</color>",
                    forceOverride = true,
                    overrideDuration = 0.3f,
                });

                if (_outOfRangeTimer > loseTimeThresh) {
                    EventBus.Post(new GeneralHintEvent {
                        msg = "MISSION FAILED!",
                        forceOverride = true,
                        overrideDuration = 0.3f,
                    });

                    _missionStarted = false;

                    player.money = Mathf.Max(0, player.money - moneyPenalty.random);

                    _inCD = true;
                }
            } else {
                EventBus.Post(new GeneralHintEvent {
                    msg = $"Accompany Jessica for {accompanyTime} seconds",
                    forceOverride = true,
                    overrideDuration = 0.1f
                });

                _outOfRangeTimer = 0f;
                _accompanyTimer += Time.deltaTime;

                if (_accompanyTimer >= accompanyTime) {
                    EventBus.Post(new GeneralHintEvent {
                        msg = $"Mission success!",
                        forceOverride = true,
                    });
                    _missionStarted = false;
                    _accompanyTimer = 0f;
                    player.money += moneyBonus.random;
                    _coinSound.Play();

                    _inCD = true;
                }
            }
        } else {
            _moving = false;
            _velocity = Vector3.zero;
        }

        if (_inCD) {
            if (_interact.enabled)
                _interact.enabled = false;

            _cdTimer += Time.deltaTime;
            if (_cdTimer > cd) {
                _cdTimer = 0;
                _inCD = false;
            }
        } else if (!_missionStarted) {
            var shouldEnable = GameContext.Instance.playerCtl.money >= canBeginMoneyThresh;
            if (_interact.enabled != shouldEnable)
                _interact.enabled = shouldEnable;
        }
    }

    [SubscribeEvent]
    private void _OnInteract(InteractEvent ev) {
        if (ev.interactable == _interact) {
            _missionStarted = true;
            _interact.enabled = false;
            _outOfRangeTimer = 0f;
            _accompanyTimer = 0f;
        }
    }
}
