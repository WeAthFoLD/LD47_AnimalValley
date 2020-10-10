using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StageName {
    Exterior,
    VirtualGame
}

public class SwapStageEvent {
    public StageName newStage;
}

public class StageManger : MonoListenEventBehaviour, ISingleton {

    public static StageManger Instance { get; private set; }

    public StageName currentStage { get; private set; }

    [SerializeField]
    private StageLoadInfo exteriorStage;
    [SerializeField]
    private StageLoadInfo virtualGameStage;

    [Serializable]
    public struct StageLoadInfo {
        public GameObject prefab;
        public Material skybox;
    }

    struct StageInfo {
        public GameObject instance;
        public Transform spawnPoint;
        public Material skybox;
    }

    private Dictionary<StageName, StageInfo> _stageInfos = new Dictionary<StageName, StageInfo>();

    public void Init(MonoGameManager manager) {
        Instance = this;
        _AddStage(StageName.Exterior, exteriorStage);
        _AddStage(StageName.VirtualGame, virtualGameStage);
    }

    [SubscribeEvent(priority = 100)]
    private void _PostInit(PostGameInitEvent evt) {
        SwapStage(StageName.VirtualGame);
    }

    private void _AddStage(StageName name, StageLoadInfo loadInfo) {
        var instance = Instantiate(loadInfo.prefab);
        var spawnPoint = instance.transform.Find("SpawnPoint");

        instance.gameObject.SetActive(false);
        _stageInfos.Add(name, new StageInfo {
            instance = instance,
            spawnPoint = spawnPoint,
            skybox = loadInfo.skybox
        });
    }

    public void SwapStage(StageName stage, string overrideSpawnPoint = null) {
        currentStage = stage;
        foreach (var entry in _stageInfos) {
            var isTarget = entry.Key == stage;
            entry.Value.instance.SetActive(isTarget);

            if (isTarget) {
                RenderSettings.skybox = entry.Value.skybox;
                var player = GameContext.Instance.player;
                Transform spawnPoint;
                if (string.IsNullOrEmpty(overrideSpawnPoint)) {
                    spawnPoint = entry.Value.spawnPoint;
                } else {
                    spawnPoint = entry.Value.instance.transform.Find(overrideSpawnPoint);
                }
                var pos = spawnPoint.position;
                player.GetComponent<Rigidbody>().position = pos;
                player.transform.position = pos;
            }
        }
        EventBus.Post(new CameraPositionResetEvent());
        EventBus.Post(new SwapStageEvent { newStage = stage });
    }

    public GameObject GetStageInstance(StageName n) {
        return _stageInfos[n].instance;
    }
}