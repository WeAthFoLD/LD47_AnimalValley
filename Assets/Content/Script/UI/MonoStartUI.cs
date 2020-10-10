
using System;
using UnityEngine.UI;

public class MonoStartUI : MonoListenEventBehaviour {
    private const string CTRL_KEY = "StartUI";

    public Button okBtn;

    private void Start() {
        okBtn.BindViewCallback(() => {
            Destroy(gameObject);
            GameContext.Instance.gameStarted = true;
            GameContext.Instance.playerCtl.disableControlCount.Remove(CTRL_KEY);
        });
    }

    [SubscribeEvent(priority = -1000)]
    private void _OnGameInit(GameInitEvent ev) {
        GameContext.Instance.playerCtl.disableControlCount.Add(CTRL_KEY);
    }

}
