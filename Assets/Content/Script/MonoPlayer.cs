using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerDieEvent {
}

public class InteractableChangedEvent {
}

public class InteractEvent {
    public MonoInteractable interactable;
}

public class MoneyChangeEvent {
}

public class MonoPlayer : MonoListenEventBehaviour {
    [SerializeField] float moveSpeed;
    [SerializeField] float moveAcc, moveDeacc;
    [SerializeField] private float initHunger;
    [SerializeField] private float hungerDepleteSpeed;
    [SerializeField] private GameObject bucketRoot;

    public HashSet<string> disableControlCount = new HashSet<string>();

    [ShowInInspector]
    public PlayerHungerModule hungerModule;
    public readonly Inventory inventory = new Inventory();

    private int _money;

    [ShowInInspector, DisableInEditorMode]
    public int money {
        get { return _money;  }
        set {
            _money = value;
            EventBus.Post(new MoneyChangeEvent());
        }
    }

    public bool dead = false;

    public MonoInteractable currentInteractable { get; private set; }

    private Rigidbody _rb;

    private Transform _missionHint;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _missionHint = transform.Find("_MissionHint");

        hungerModule = new PlayerHungerModule(this);

        hungerModule.Init();
    }

    public void Die() {
        if (dead)
            return;

        dead = true;
        EventBus.Post(new PlayerDieEvent());

        if (StageManger.Instance.currentStage != StageName.Exterior)
            StageManger.Instance.SwapStage(StageName.Exterior, "ComputerSpawn");
    }

    public void StandMissionStart() {
        disableControlCount.Add(MonoStandMission.CTRL_KEY);
        currentInteractable = null;
        EventBus.Post(new InteractableChangedEvent());
    }

    private int _carryRefCount;

    public void CarryMission(bool enable) {
        _carryRefCount += (enable ? 1 : -1);
        bucketRoot.LasySetActive(_carryRefCount > 0);
    }

    // Start is called before the first frame update
    void Start() {
    }

    private void Update() {
        if (GameContext.Instance && GameContext.Instance.gameStarted && !GameContext.Instance.gameFinal)
            hungerModule.Update();

        var input = InputUtil.Player;
        if (input == null || dead || disableControlCount.Count > 0 || !StageManger.Instance)
            return;

        if (input.GetButtonDown(InputUtil.Interact) && currentInteractable) {
            currentInteractable.OnInteract();
        }

        bool inMain = StageManger.Instance.currentStage == StageName.Exterior;
        _missionHint.gameObject.LasySetActive(!inMain && MissionManager.currentMission);
        if (!inMain) {
            if (_missionHint.gameObject.activeSelf) {
                var dpos = MissionManager.currentMission.transform.position - transform.position;
                _missionHint.transform.localRotation = Quaternion.Euler(90f, 90f -Mathf.Atan2(dpos.z, dpos.x) * Mathf.Rad2Deg, 0f);
            }
        }
    }

    void FixedUpdate() {
        var vel = _rb.velocity;
        var input = InputUtil.Player;
        if (input == null || !GameContext.Instance || !GameContext.Instance.mainCamera)
            return;

        if (dead || disableControlCount.Count > 0) {
            vel.x = vel.z = 0f;
            _rb.velocity = vel;
            return;
        }

        var mainCamera = GameContext.Instance.mainCamera.transform;
        var xz = new Vector2(vel.x, vel.z);

        var fwd = mainCamera.forward;
        fwd.y = 0;
        fwd = fwd.normalized;

        var right = new Vector3(fwd.z, 0, -fwd.x);

        var targetDir3 = (right * input.GetAxis(InputUtil.AxisX)) + (fwd * input.GetAxis(InputUtil.AxisY));
        Vector2 targetDir = new Vector2(targetDir3.x, targetDir3.z);

        float acc, targetSpeed;
        if (targetDir.sqrMagnitude > .1f) {
            targetDir = targetDir.normalized;
            // xz = targetDir * Vector2.Dot(xz, targetDir);
            acc = moveAcc;
            targetSpeed = moveSpeed;
        } else {
            targetDir = xz;
            acc = moveDeacc;
            targetSpeed = .0f;
        }

        var curSpeed = xz.magnitude;
        var newSpeed = Mathf.MoveTowards(curSpeed, targetSpeed, acc * Time.deltaTime);
        if (targetDir.sqrMagnitude > .1f) {
            var newVelXZ = newSpeed * targetDir.normalized;
            _rb.velocity = new Vector3(newVelXZ.x, vel.y, newVelXZ.y);
        }

        if (!(currentInteractable is null) && (!currentInteractable || !currentInteractable.isActiveAndEnabled)) {
            currentInteractable = null;
            EventBus.Post(new InteractableChangedEvent());
        }
    }

    [SubscribeEvent(priority = -100)]
    void _OnPostInit(PostGameInitEvent evt) {
        _rb.useGravity = true;
    }

    void OnTriggerEnter(Collider other) {
        if (LayerMaskUtil.GetLayerMask(MyLayers.Interactable).Contains(other.gameObject.layer)) {
            var interactble = other.gameObject.GetComponent<MonoInteractable>();
            if (interactble && interactble.enabled) {
                currentInteractable = interactble;
                EventBus.Post(new InteractableChangedEvent());
            }
        }
    }

    void OnTriggerExit(Collider other) {
        if (LayerMaskUtil.GetLayerMask(MyLayers.Interactable).Contains(other.gameObject.layer)) {
            if (other.GetComponent<MonoInteractable>() == currentInteractable) {
                currentInteractable = null;
                EventBus.Post(new InteractableChangedEvent());
            }
        }
    }

    public class PlayerHungerModule {
        [ShowInInspector]
        public float currentHunger { get; private set; }
        public float maxHunger { get; private set; }

        // 如果吃了一个食物 会优先加 currentHunger 然后加overflowHunger max用来算进度显示
        [ShowInInspector]
        public float overflowHunger;
        public float maxOverflowHunger;

        private MonoPlayer _owner;

        public PlayerHungerModule(MonoPlayer owner) {
            _owner = owner;
        }

        public void Init() {
            maxHunger = _owner.initHunger;
            SetHunger(maxHunger);
        }

        public void SetHunger(float value) {
            currentHunger = Mathf.Clamp(value, 0, maxHunger);

            if (currentHunger <= 0f)
                _owner.Die();
        }

        public void DrainHunger(float value) {
            XDebug.Assert(value >= 0);
            if (overflowHunger > 0) {
                if (overflowHunger > value) {
                    overflowHunger -= value;
                    value = 0f;
                } else {
                    overflowHunger = 0f;
                    value -= overflowHunger;
                }
            }

            SetHunger(currentHunger - value);
        }

        public void AddHunger(float foodHungerValue) {
            float addHunger = foodHungerValue;
            float canAdd = maxHunger - currentHunger;
            if (canAdd > addHunger) {
                SetHunger(currentHunger + addHunger);
                addHunger = 0;
            } else {
                SetHunger(maxHunger);
                addHunger -= canAdd;
            }

            maxOverflowHunger = foodHungerValue;
            overflowHunger = addHunger;
        }

        public void Update() {
            DrainHunger(Time.deltaTime * _owner.hungerDepleteSpeed);
        }
    }
}