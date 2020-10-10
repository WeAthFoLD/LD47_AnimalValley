using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AnimatorTransition {

// State Behaviour is attached in TransitionProfile state or portion,
//  to execute certain actions in a fixed state time range.
public abstract class StateBehaviour : ScriptableObject {
    
    [NonSerialized] 
    internal TransitionRuntime _runtime;

    protected TransitionRuntime runtime {
        get {
            return _runtime;
        }
    }

    protected TransitionRuntime.CurrentStateInfo currentState {
        get {
            return runtime.currentState;
        }
    }

    protected Transform transform {
        get {
            return runtime.transform;
        }
    }

    protected GameObject gameObject {
        get {
            return runtime.gameObject;
        }
    }

    protected T GetComponent<T>() {
        return runtime.GetComponent<T>();
    }

    public virtual void Init() {}

    public virtual void Destroy() { }

    public virtual void Enter() {}

    public virtual void Update() {}

    public virtual void FixedUpdate() {}

    public virtual void LateUpdate() {}

    public virtual void FrameUpdate(int frame) {}

    public virtual void Exit() {}

    protected void print(object message) {
        Debug.Log(message);
    }

    protected Coroutine StartCoroutine(IEnumerator coro) {
        return runtime.StartCoroutine(coro);
    }

}

}