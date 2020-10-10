using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonoMissionHintUI : MonoListenEventBehaviour {
    private const int MissionCount = 8;

    private Transform[] _items;

    private float timer = 0f;

    void Awake() {
        _items = new Transform[MissionCount];
        for (int i = 0; i < MissionCount; ++i)
            _items[i] = transform.GetChild(i);
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= 0.3f) {
            timer = 0f;
            Refresh();
        }
    }

    [SubscribeEvent(priority = -100)]
    private void GameInit(GameInitEvent ev) {
        Refresh();
    }

    private void Refresh() {
        var missionList = MissionManager.GetAllActiveMission();
        for (int i = 0; i < MissionCount; ++i) {
            var mission = i >= missionList.Count ? null : missionList[i];
            if (StageManger.Instance.currentStage == StageName.Exterior || mission == null) {
                _items[i].gameObject.SetActive(false);
            } else {
                _items[i].gameObject.SetActive(true);
                _items[i].GetChild(0).GetComponent<Text>().text = mission.missionName;
                var btn = _items[i].GetComponent<Button>();
                btn.interactable = mission != MissionManager.currentMission;
                btn.BindViewCallback(() => {
                    MissionManager.currentMission = mission;
                    Refresh();
                });
            }
        }
    }
}