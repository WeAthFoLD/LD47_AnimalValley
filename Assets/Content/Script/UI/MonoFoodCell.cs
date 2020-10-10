using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonoFoodCell : MonoBehaviour {
    public Image fill;

    public void Setup(float x) {
        gameObject.SetActive(x > 0);
        if (x > 0) {
            fill.fillAmount = x;
        }
    }
}
