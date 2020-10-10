using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

public enum PollutionState {
    None,
    Sandstorm,
    LandPollution,
    LandPollution2,
}

public class GameFinalEvent {
}

public static class MissionManager {

    private static List<MonoMissionHint> _allMissions = new List<MonoMissionHint>();

    public static MonoMissionHint currentMission;

    public static void Init() {
        _allMissions.Clear();
    }

    public static void Subscribe(MonoMissionHint hint) {
        _allMissions.Add(hint);
    }

    public static List<MonoMissionHint> GetAllActiveMission() {
        return _allMissions.Where(x => x && x.interactable && x.interactable.enabled)
            .ToList();
    }

    public static void Update() {
        if (!currentMission || !currentMission.interactable || !currentMission.interactable.enabled)
            currentMission = null;
    }

}

public class GameContext : MonoListenEventBehaviour, ISingleton {

    public static GameContext Instance { get; private set; }

    public AllItemConfig allItemConfig;

    public ItemRegistry itemRegistry;

    public Canvas mainCanvas;

    public GameObject playerPrefab;

    public int[] pollutionMoneyThresh;
    public string[] pollutionStateDesc;

    [NonSerialized, ShowInInspector, HideInEditorMode]
    public PollutionState pollutionState = PollutionState.None;

    [NonSerialized]
    public MonoMainCamera mainCamera;
    [NonSerialized]
    public GameObject player;

    [NonSerialized] public MonoPlayer playerCtl;

    [NonSerialized] public bool gameStarted;
    // 是否获取了VR设备并进入虚拟世界
    public bool gameFinal { get; private set; }

    private MonoGameManager _gameManager;

    private bool _hungryMissonFinished = false;

    private StudioEventEmitter _finalSound;

    public void Init(MonoGameManager manager) {
        _gameManager = manager;
        Instance = this;

        _finalSound = gameObject.AddComponent<StudioEventEmitter>();
        _finalSound.Event = "event:/vr_boot";

        itemRegistry = new ItemRegistry();
        foreach (var item in allItemConfig.items) {
            itemRegistry.Add(item);
        }

        AllItems.Init(itemRegistry);
    }

    public void SetGameFinal() {
        if (!gameFinal) {
            gameFinal = true;
            _finalSound.Play();
            EventBus.Post(new GameFinalEvent());
            EventBus.Post(new GeneralHintEvent { msg = "You now live happily in Animal Valley forever." });
        }
    }

    [SubscribeEvent(priority = 1000)]
    void _OnGameInit(GameInitEvent evt) {
        mainCamera = GameObject.Find("Main Camera").GetComponent<MonoMainCamera>();
        player = Instantiate(playerPrefab);
        playerCtl = player.GetComponent<MonoPlayer>();

        InputUtil.Init();

        mainCamera.target = player.transform;
        EventBus.Post(new CameraPositionResetEvent());
    }

    private void SetPollutionState(PollutionState st) {
        if (st <= pollutionState)
            return;
        pollutionState = st;

        StartCoroutine(CoroDisplayHintMessage(pollutionStateDesc[(int) st - 1]));
    }

    IEnumerator CoroDisplayHintMessage(string msg) {
        yield return new WaitForSeconds(UnityEngine.Random.Range(5.0f, 8.0f));
        EventBus.Post(new GeneralHintEvent { msg = msg });
    }

    private void Update() {
        if (!Instance)
            return;
        if (playerCtl) {
            if (pollutionState == PollutionState.None && playerCtl.money > pollutionMoneyThresh[0])
                SetPollutionState(PollutionState.Sandstorm);
            if (pollutionState == PollutionState.Sandstorm && playerCtl.money > pollutionMoneyThresh[1])
                SetPollutionState(PollutionState.LandPollution);
            if (pollutionState == PollutionState.LandPollution && playerCtl.money > pollutionMoneyThresh[2])
                SetPollutionState(PollutionState.LandPollution2);
        }

        if (!_hungryMissonFinished) {
            if (StageManger.Instance.currentStage == StageName.Exterior) {
                _hungryMissonFinished = true;
            } else if (playerCtl.hungerModule.currentHunger < 4) {
                _hungryMissonFinished = true;
                EventBus.Post(new GeneralHintEvent {
                    msg = "(↖) You are hungry. Quit the game (↗) to get some food",
                    forceOverride = true,
                    overrideDuration = 5f,
                    layer = 1
                });
            }
        }

        MissionManager.Update();
    }
}
