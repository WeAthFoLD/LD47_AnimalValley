using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace AnimatorTransition {

[CreateAssetMenu]
public class TransitionProfile : ScriptableObject {

#if UNITY_EDITOR
    public AnimatorController controller;
#endif
    public int controllerLayer;

    public List<string> tags = new List<string>();
    public List<State> states = new List<State>();
    public List<Param> parameters = new List<Param>();
    public List<Transition> transitions = new List<Transition>();

    public State FindState(string name) {
        foreach (var st in states) { 
            if (st.stateName == name) {
                return st;
            }
        }
        return null;
    }

    public Param FindParam(string name) {
        foreach (var param in parameters) {
            if (param.name == name)
                return param;
        }
        return null;
    }

    public TransitionProfile CreateRuntimeClone() {
        TransitionProfile ret = Instantiate(this) as TransitionProfile;

        // Deep clone states and parameters
        for (int i = 0; i < ret.states.Count; ++i) {
            ret.states[i] = CloneState(ret, ret.states[i]);
        }

        for (int i = 0; i < ret.parameters.Count; ++i) {
            ret.parameters[i] = new Param(ret.parameters[i]);
        }

        return ret;
    }

    State CloneState(TransitionProfile p, State s) {
        var ret = Instantiate(s) as State;
        ret.profile = p;
        ret.behaviours = CloneBehaviourList(ret.behaviours, s);

        foreach (var portion in ret.portions) {
            portion.behaviours = CloneBehaviourList(portion.behaviours, s);
        }

        return ret;
    }

    List<StateBehaviour> CloneBehaviourList(List<StateBehaviour> list, State s) {
        List<StateBehaviour> ret = new List<StateBehaviour>();
        foreach (var template in list) {
            if (!template) {
                Debug.LogError("Template StateBehaviour is null, state=" + s.stateName);
            } else {
                ret.Add(Instantiate(template));
            }
        }
        return ret;
    }

}

}
