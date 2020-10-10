using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MonoInventoryItem : MonoBehaviour {

    public Image icon;
    public Text textCount;
    public Text textName;
    public Text textDesc;
    public Button useButton;

    public void SetupView(ItemStack stack, UnityAction useCallback) {
        icon.sprite = stack.item.sprite;
        textCount.text = "x" + stack.number;
        textName.text = stack.item.name;
        textDesc.text = stack.item.desc;

        if (stack.item == AllItems.food)
            useButton.interactable = GameContext.Instance.playerCtl.hungerModule.overflowHunger <= 0f;
        else
            useButton.interactable = true;
        useButton.BindViewCallback(useCallback);
    }

}
