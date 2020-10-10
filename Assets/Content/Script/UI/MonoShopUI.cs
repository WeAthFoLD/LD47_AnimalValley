using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using SPlay;
using UnityEngine;
using UnityEngine.UI;

public class MonoShopUI : MonoListenEventBehaviour {
    private const string CTRL_KEY = "Shop";

    private class ShopEntry {
        public string item;
        public Func<int> costFn;
        public bool onlyOnce;
        public bool soldOut;

        public ShopEntry(string item, Func<int> costFn) {
            this.item = item;
            this.costFn = costFn;
        }
    }

    private static List<ShopEntry> entries = new List<ShopEntry> {
        new ShopEntry("food", () => {
            var pollute = GameContext.Instance.pollutionState;
            var baseCost = 320;
            if (pollute == PollutionState.Sandstorm)
                baseCost *= 2;
            else if (pollute == PollutionState.LandPollution)
                baseCost *= 4;
            else if (pollute == PollutionState.LandPollution2)
                baseCost *= 8;
            return baseCost;
        }),
        new ShopEntry("entry_ticket", () => {
            var money = GameContext.Instance.playerCtl.money;
            if (money >= 100000)
                return 10000000;
            else
                return 100000;
        }) { onlyOnce = true },
        new ShopEntry("vr_equip", () => 100000) { onlyOnce = true }
    };

    public GameObject itemPrefab;

    public Transform contentRoot;

    public Button closeBtn;

    private MonoPlayer _owner;

    public void SetupView(MonoPlayer player) {
        _owner = player;
        RefreshView();

        _owner.disableControlCount.Add(CTRL_KEY);

        closeBtn.BindViewCallback(Close);
    }

    private void RefreshView() {
        foreach (Transform child in contentRoot) {
            Destroy(child.gameObject);
        }

        for (var i = 0; i < entries.Count; i++) {
            var entry = entries[i];
            var instance = Instantiate(itemPrefab, contentRoot);
            var item = GameContext.Instance.itemRegistry.GetItem(entry.item);
            var cost = entry.costFn();
            instance.GetComponent<MonoShopItem>().SetupView(
                item,
                cost,
                entry.soldOut, () => {
                var stack = new ItemStack(item, 1);
                _owner.inventory.Add(stack);
                if (entry.onlyOnce) {
                    entry.soldOut = true;
                }

                _owner.money -= cost; // Will trigger RefreshView()
                EventBus.Post(new GeneralHintEvent { msg = $"Bought item {item.name} x1", overrideDuration = 1.5f });
            });
        }
    }

    private void Close() {
        gameObject.SetActive(false);
        _owner.disableControlCount.Remove(CTRL_KEY);
    }

    [SubscribeEvent]
    private void _OnMoneyChange(MoneyChangeEvent ev) {
        RefreshView();
    }

    [SubscribeEvent]
    private void _OnFinalGame(GameFinalEvent ev) {
        // 将食物标记为soldOut
        entries.Where(x => x.item == "food")
            .ForEach(x => x.soldOut = true);
    }

}
