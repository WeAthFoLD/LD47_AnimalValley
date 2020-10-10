using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonoPlayerValuesUI : MonoListenEventBehaviour {

    public GameObject hungerBar;
    public Transform hungerFill;
    public Transform hungerExtraFill;
    public Text moneyText;

    private MonoFoodCell[] hungerFillCells;
    private MonoFoodCell[] hungerExtraFillCells;

    private void Awake() {
        hungerFillCells = new MonoFoodCell[5];
        hungerExtraFillCells = new MonoFoodCell[5];
        for (int i = 0; i < 5; ++i) {
            hungerFillCells[i] = hungerFill.GetChild(i).GetComponent<MonoFoodCell>();
            hungerExtraFillCells[i] = hungerExtraFill.GetChild(i).GetComponent<MonoFoodCell>();
        }
    }

    void Update() {
        if (GameContext.Instance == null)
            return;
        var player = GameContext.Instance.player;
        if (!player)
            return;
        var playerCtrl = player.GetComponent<MonoPlayer>();

        var hungerModule = playerCtrl.hungerModule;

        SetupCells(hungerFillCells, hungerModule.currentHunger);
        SetupCells(hungerExtraFillCells, hungerModule.overflowHunger);
        moneyText.text = "$" + playerCtrl.money;
    }

    void SetupCells(MonoFoodCell[] cells, float val) {
        int flr = Mathf.FloorToInt(val);
        for (int i = 0; i < cells.Length; ++i) {
            float remain = val - i;
            if (remain > 0f) {
                cells[i].Setup(i == flr ? (val - flr) : 1f);
            } else {
                cells[i].Setup(0f);
            }
        }
    }

    [SubscribeEvent]
    private void _OnGameFinal(GameFinalEvent ev) {
        hungerBar.SetActive(false);
    }

}