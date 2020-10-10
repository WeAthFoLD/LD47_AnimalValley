using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonoUIBar : MonoListenEventBehaviour {
    public Button inventoryBtn, shopBtn, quitBtn;

    void Start() {
        inventoryBtn.BindViewCallback(() => {
            var invPage = GameContext.Instance.mainCanvas.transform.Find("InventoryUI");
            var enabled = invPage.gameObject.activeSelf;
            invPage.gameObject.SetActive(!enabled);
            if (!enabled) {
                invPage.GetComponent<MonoInventoryUI>().SetupView(GameContext.Instance.playerCtl);
            }
        });

        shopBtn.BindViewCallback(() => {
            var invPage = GameContext.Instance.mainCanvas.transform.Find("ShopUI");
            var enabled = invPage.gameObject.activeSelf;
            invPage.gameObject.SetActive(!enabled);
            if (!enabled) {
                invPage.GetComponent<MonoShopUI>().SetupView(GameContext.Instance.playerCtl);
            }
        });

        quitBtn.BindViewCallback(() => {
            if (GameContext.Instance.gameFinal) {
                EventBus.Post(new GeneralHintEvent {
                    msg = "You live happily in Animal Valley forever, so you can't leave.",
                    overrideDuration = 3f
                });
            } else {
                StageManger.Instance.SwapStage(StageName.Exterior, "ComputerSpawn");
            }
        });
    }

    [SubscribeEvent]
    private void _OnStageSwap(SwapStageEvent ev) {
        bool isVirtual = ev.newStage == StageName.VirtualGame;
        inventoryBtn.gameObject.SetActive(!isVirtual);
        shopBtn.gameObject.SetActive(isVirtual);
        quitBtn.gameObject.SetActive(isVirtual);
    }
}