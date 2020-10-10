using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MonoShopItem : MonoBehaviour {

    public Image icon;
    public Text textCost;
    public Text textName;
    public Text textDesc;
    public Button useButton;

    public void SetupView(Item item, int cost, bool soldOut, UnityAction useCallback) {
        icon.sprite = item.sprite;
        textCost.text = "$" + cost.ToString();
        textName.text = item.name;
        textDesc.text = item.desc;

        var canBuy = !soldOut && GameContext.Instance.playerCtl.money >= cost;
        useButton.interactable = canBuy;
        useButton.BindViewCallback(useCallback);
        useButton.GetComponentInChildren<Text>().text = soldOut ? "Sold Out" : (canBuy ? "Buy" : "Can't Buy");
    }

}
