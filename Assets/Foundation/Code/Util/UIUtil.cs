using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIUtil {

    public static void BindViewCallback(this Button btn, UnityAction callback) {
        btn.onClick.RemoveAllListeners();
        if (callback != null)
            btn.onClick.AddListener(callback);
    }

}
