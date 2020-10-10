using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AnimatorTransition {

public enum FromStateType {
    State, Tag, Any
}

[Serializable]
public class FromStateFilter {
    public FromStateType type = FromStateType.State;
    public string stateOrTagName = "";
    public string portionName;

    private bool _runtimeInit;
    private int _stateOrTagHash = -1;
    private int _portionHash = -1;

    public bool Accepts(TransitionRuntime.CurrentStateInfo info) {
        if (type == FromStateType.Any)
            return true;

        if (!_runtimeInit) {
            if (type == FromStateType.State || type == FromStateType.Tag) {
                _stateOrTagHash = Animator.StringToHash(stateOrTagName);
            }

            if (!string.IsNullOrEmpty(portionName))
                _portionHash = Animator.StringToHash(portionName);

            _runtimeInit = true;
        }

        var state = info.state;
        var frame = info.frame;

        if (type == FromStateType.Tag) {
            if (state.HasTag(frame, _stateOrTagHash))
                return true;
        }

        // type == State
        if (info.stateInfo.shortNameHash != _stateOrTagHash)
            return false;

        if (_portionHash == -1) {
            return true;
        }

        return state.IsPortion(_portionHash, frame);
    }

    public string GetStatePreview() {
        if (type == FromStateType.Any) {
            return "Any";
        }
        if (type == FromStateType.State) {
            return "State(" + stateOrTagName + ")";
        }
        
        return "Tag(" + stateOrTagName + ")";
    }
}

public enum Cmp {
    Greater, Less, Equal, NotEqual,
    GreaterEqual, LessEqual
}

[Serializable]
public class Condition {
    public string name;
    public Cmp cmp;
    public bool boolValue = true;
    public float floatValue;
    public int intValue;

    // Runtime only, 只需要被初始化一次就行
    public int nameIndex;

    public bool Evaluate(TransitionRuntime runtime, float delay) {
        var param = runtime.GetParam(nameIndex);
        if (param == null) {
            throw new Exception("Parameter " + name + " doesn't exist.");
        }

        var type = param.type;
        switch (type) {
            case ParamType.Bool:
                return param.boolValue == boolValue || (boolValue ? -param.delayTimer : param.delayTimer) < delay;
            case ParamType.Trigger:
                return param.boolValue || -param.delayTimer < delay;
            case ParamType.Float: {
                float val = param.floatValue;
                switch (cmp) {
                    case Cmp.Equal:    return val == floatValue;
                    case Cmp.Greater:  return val >  floatValue;
                    case Cmp.Less:     return val <  floatValue;
                    case Cmp.NotEqual: return val != floatValue;
                    case Cmp.GreaterEqual: return val >= floatValue;
                    case Cmp.LessEqual: return val <= floatValue;
                    default: throw new Exception("WTF");
                }
            }
            case ParamType.Int: {
                int val = param.intValue;
                switch (cmp) {
                    case Cmp.Equal:     return val == intValue;
                    case Cmp.Greater:   return val >  intValue;
                    case Cmp.Less:      return val <  intValue;
                    case Cmp.NotEqual:  return val != intValue;
                    case Cmp.GreaterEqual: return val >= intValue;
                    case Cmp.LessEqual:    return val <= intValue;
                    default: throw new Exception("WTF");
                }
            }
            default: throw new Exception("Invalid parameter type for " + name);
        }
    }
}

public enum TriggerRangeType {
    AnyTime, StateEnd, Range, FrameSinceExec, FrameSinceExecBefore
}

public enum ActionType {
    ChangeState, SendMessage
}

public enum MessageParType {
    None, Int, Float, Bool
}

[Serializable]
public class Transition : ScriptableObject {
    public TransitionProfile profile;
    public bool enabled = true;
    
    public FromStateFilter fromState = new FromStateFilter();
    public List<Condition> conditions = new List<Condition>();

    public TriggerRangeType triggerRangeType = TriggerRangeType.AnyTime;
    public IntRange triggerRange;
    public int triggerFrameSinceExec;

    public float timeBuffer;

    public ActionType actionType = ActionType.ChangeState;
    public string targetStateName = "";
    public int targetStateFrame; 
    public int priority = 0;

    public bool shouldDelay;
    public float delay;

    public string messageName = "";
    public MessageParType messageParType = MessageParType.None;

    public float messageParFloat;
    public int messageParInt;
    public bool messageParBool;

    public string targetInfo {
        get {
            if (actionType == ActionType.ChangeState) {
                return "State(" + targetStateName + ")";
            } else {
                return "Msg(" + messageName + ")";
            }
        }
    }

    public bool InTriggerRange(TransitionRuntime.CurrentStateInfo info) {
        if (triggerRangeType == TriggerRangeType.AnyTime)
            return true;
        if (triggerRangeType == TriggerRangeType.StateEnd)
            return info.stateInfo.normalizedTime >= 1;
        if (triggerRangeType == TriggerRangeType.FrameSinceExec)
            return FrameUtil.Time2Frame(info.timeSinceExecution) >= triggerFrameSinceExec;
        if (triggerRangeType == TriggerRangeType.FrameSinceExecBefore)
            return FrameUtil.Time2Frame(info.timeSinceExecution) <= triggerFrameSinceExec;

        return triggerRange.Contains(info.frame);
    }
    
}

}