using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AnimatorTransition {
public enum ParamType {
    Int, Float, Bool, Trigger
}

[Serializable]
public class Param {
    public string name = "";
    public ParamType type;
    public bool boolValue;
    public int intValue;
    public float floatValue;

    public float delayTimer = -100f;

    public Param() {}

    public Param(Param other) {
        this.name = other.name;
        this.type = other.type;
        this.boolValue = other.boolValue;
        this.intValue = other.intValue;
        this.floatValue = other.floatValue;
    }
}
}
