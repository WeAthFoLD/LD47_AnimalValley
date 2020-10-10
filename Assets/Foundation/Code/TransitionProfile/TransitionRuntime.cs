using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimatorTransition {

[RequireComponent(typeof(Animator))]
public class TransitionRuntime : MonoBehaviour {
    
    public struct CurrentStateInfo {
        public State state;
        public AnimatorStateInfo stateInfo;
        public int frame;
        public int totalFrame;

        public float timeSinceExecution;

        public bool HasTag(int tagHash) {
            return state != null && state.HasTag(frame, tagHash);
        }

        public bool IsName(string name) {
            return stateInfo.IsName(name);
        }

        public bool IsInPortion(int nameHash) {
            return state.IsPortion(nameHash, frame);
        }

        public float normalizedTime {
            get { return stateInfo.normalizedTime; }
        }

        public string stateName {
            get { return state.stateName; }
        }
    }

    private class QueuedTransition {
        public float delay;
        public Transition transition;
    }

    private class TransitionNode {
        public Transition transition;
    }

    #region PublicFields

    public TransitionProfile profile;

    public readonly List<Param> paramTable = new List<Param>();

    public Action onStateChanged;

    public CurrentStateInfo currentState {
        get; private set;
    }

    #endregion

    #region PrivateFields

    private Animator _animator;

    private readonly Dictionary<int, State> _stateTable = new Dictionary<int, State>();

    private readonly List<TransitionNode> _transitionList = new List<TransitionNode>();

    private List<List<StateBehaviour>>
        lastFrameBehaviours = new List<List<StateBehaviour>>(),
        currentBehaviours = new List<List<StateBehaviour>>();

    private readonly List<QueuedTransition> _queuedTransitions = new List<QueuedTransition>();

    private int _lastTransitFrameCount = -1;
    private int _lastTransitPrio = -1;
    private string _lastTransitState = "";

    #endregion

    #region PublicMethod

    public int GetParamIndex(string name) {
        for (var i = 0; i < paramTable.Count; i++) {
            var param = paramTable[i];
            if (param.name == name)
                return i;
        }

        return -1;
    }

    public Param GetParam(int index) {
        return paramTable[index];
    }

    public void SetInt(int index, int val) {
        GetParam(index).intValue = val;
    }

    public void SetFloat(int index, float val) {
        GetParam(index).floatValue = val;
    }

    public void SetBool(int index, bool val) {
        GetParam(index).boolValue = val;
    }

    public float GetFloat(int index) {
        return GetParam(index).floatValue;
    }

    public int GetInt(int index) {
        return GetParam(index).intValue;
    }

    public bool GetBool(int index) {
        return GetParam(index).boolValue;
    }

    public bool GetBoolDelayed(int index, float delay) {
        var param = GetParam(index);
        return param.boolValue || -param.delayTimer < delay;
    }

    public bool GetBoolNegateDelayed(int index, float delay) {
        var param = GetParam(index);
        return !param.boolValue || param.delayTimer < delay;
    }

    public void AddTransition(Transition trans) {
        _transitionList.Add(new TransitionNode {
            transition = trans
        });
    }

    public void RemoveTransition(Transition trans) {
        _transitionList.RemoveAll(it => it.transition == trans);
    }

    public State FindState(string name) {
        return _stateTable[Animator.StringToHash(name)];
    }

    public void Play(string name, int priority = 0, float normalizedTime = 0) {
        _queuedTransitions.Clear();
        if (_lastTransitFrameCount == Time.frameCount) {
            if (priority < _lastTransitPrio) {
                return;
            } else if (priority == _lastTransitPrio && _lastTransitState != name) {
                Debug.LogWarningFormat("animator.Play() collision: trying to play {0}, already played {1}, priority {2}", name, _lastTransitState, priority);
            }
        } 

        _lastTransitFrameCount = Time.frameCount;
        _lastTransitPrio = priority;
        _lastTransitState = name;

        _animator.Play(name, profile.controllerLayer, normalizedTime);
    }

    #endregion

    #region PrivateMethod

    private void Awake() {
        _animator = GetComponent<Animator>();
        profile = profile.CreateRuntimeClone();

        foreach (var p in profile.parameters) {
            paramTable.Add(new Param(p));
        }

        // 初始化condition的name index
        foreach (var trans in profile.transitions) {
            foreach (var cond in trans.conditions) {
                cond.nameIndex = GetParamIndex(cond.name);
            }
        }
    }

    private void Start() {
        foreach (var tr in profile.transitions) {
            var node = new TransitionNode {
                transition = tr,
            };

            _transitionList.Add(node);
        }

        foreach (var state in profile.states) {
            _stateTable.Add(Animator.StringToHash(state.stateName), state);

            _InitStateBehaviours(state.behaviours);
            foreach (var portion in state.allPortions) {
                _InitStateBehaviours(portion.behaviours);
            }
        }
    }

    private void OnDestroy() {
        foreach (var state in profile.states) {
            _DestroyStateBehaviours(state.behaviours);
            foreach (var portion in state.allPortions) {
                _DestroyStateBehaviours(portion.behaviours);
            }
        }
    }

    private void Update() {
        _UpdateCurrentState(true);

        // Update bool parameters timer
        foreach (var param in paramTable) {
            if (param.boolValue)
                param.delayTimer = Mathf.Max(0, param.delayTimer) + Time.deltaTime;
            else
                param.delayTimer = Mathf.Min(0, param.delayTimer) - Time.deltaTime;
        }

        // Update states
        foreach (var list in currentBehaviours) {
            _UpdateStateBehaviours(list);
        }

        foreach (var node in _transitionList) {
            var trans = node.transition;

            if (trans.enabled && trans.fromState.Accepts(currentState)) {
                // Evaluate condition
                bool triggered = true;
                try {
                    foreach (var cond in trans.conditions) {
                        if (!cond.Evaluate(this, trans.timeBuffer)) {
                            triggered = false;
                            break;
                        }
                    }
                } catch (Exception ex) {
                    print("Exception when evaluating " + trans.fromState.GetStatePreview() + " -> " + trans.targetStateName + ", " + ex);
                    break;
                }

                // Do actual trigger
                if (trans.InTriggerRange(currentState) && triggered) {
                    if (trans.shouldDelay)
                        _queuedTransitions.Add(new QueuedTransition { transition = trans, delay = trans.delay });
                    else
                        DoTriggerTransition(trans);
                }
            }
        }

        // Delayed transition
        for (int i = _queuedTransitions.Count - 1; i >= 0; --i) {
            var item = _queuedTransitions[i];
            item.delay -= Time.deltaTime;
            if (item.delay <= 0) {
                _queuedTransitions.RemoveAt(i);
                DoTriggerTransition(item.transition);
            } else if (!item.transition.fromState.Accepts(currentState)) {
                _queuedTransitions.RemoveAt(i);
            }
        }

    }

    private void LateUpdate() {
        _UpdateCurrentState(false);

        for (var i = 0; i < currentBehaviours.Count; i++) {
            var list = currentBehaviours[i];
            for (var j = 0; j < list.Count; j++) {
                var behaviour = list[j];
                behaviour.LateUpdate();
            }
        }

        // Update state behaviour list
        var temp = lastFrameBehaviours;
        lastFrameBehaviours = currentBehaviours;
        currentBehaviours = temp;

        currentBehaviours.Clear();

        currentBehaviours.Add(currentState.state.behaviours);
        for (var i = 0; i < currentState.state.allPortions.Count; i++) {
            var portion = currentState.state.allPortions[i];
            if (portion.IsInPortion(currentState.frame)) {
                currentBehaviours.Add(portion.behaviours);
            }
        }

        // Exit states
        for (var i = 0; i < lastFrameBehaviours.Count; i++) {
            var list = lastFrameBehaviours[i];
            if (!currentBehaviours.Contains(list)) {
                _ExitStateBehaviours(list);
            }
        }

        // Enter states
        for (var i = 0; i < currentBehaviours.Count; i++) {
            var list = currentBehaviours[i];
            if (!lastFrameBehaviours.Contains(list)) {
                _EnterStateBehaviours(list);
            }
        }
    }

    private void FixedUpdate() {
        for (var i = 0; i < currentBehaviours.Count; i++) {
            _FixedUpdateStateBehaviours(currentBehaviours[i]);
        }
    }

    private void _InitStateBehaviours(List<StateBehaviour> list) {
        for (var i = 0; i < list.Count; i++) {
            var behaviour = list[i];
            behaviour._runtime = this;
            behaviour.Init();
        }
    }

    private void _EnterStateBehaviours(List<StateBehaviour> list) {
        for (var i = 0; i < list.Count; i++) {
            list[i].Enter();
        }
    }

    private void _ExitStateBehaviours(List<StateBehaviour> list) {
        for (var i = 0; i < list.Count; i++) {
            list[i].Exit();
        }
    }

    private void _UpdateStateBehaviours(List<StateBehaviour> list) {
        for (var i = 0; i < list.Count; i++) {
            list[i].Update();
        }
    }

    private void _FixedUpdateStateBehaviours(List<StateBehaviour> list) {
        for (var i = 0; i < list.Count; i++) {
            list[i].FixedUpdate();
        }
    }

    private void _FrameUpdateStateBehaviours(List<StateBehaviour> list, int frame) {
        for (var i = 0; i < list.Count; i++) {
            list[i].FrameUpdate(frame);
        }
    }

    private void _DestroyStateBehaviours(List<StateBehaviour> list) {
        for (var i = 0; i < list.Count; i++) {
            list[i].Destroy();
        }
    }

    private void _UpdateCurrentState(bool updateTime) {
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(profile.controllerLayer);
        // var frame = Mathf.Min(TimeUtils.Time2Frame(stateInfo.length * stateInfo.normalizedTime), TimeUtils.Time2Frame(stateInfo.length));
        var totalFrame = FrameUtil.Time2Frame(stateInfo.length * stateInfo.normalizedTime);
        var profileState = _stateTable[stateInfo.shortNameHash];
        var frame = stateInfo.loop ? totalFrame % profileState.frames : totalFrame;

        CurrentStateInfo prevState = currentState;
        {
            var nstate = currentState;
            var didStateChange = false;
            nstate.frame = frame;
            nstate.totalFrame = totalFrame;
            nstate.state = profileState;
            nstate.stateInfo = stateInfo;

            if (nstate.state != currentState.state) {
                nstate.timeSinceExecution = 0;
                didStateChange = true;
            } else if (updateTime) {
                nstate.timeSinceExecution += Time.deltaTime;
            }

            currentState = nstate;

            if (didStateChange && onStateChanged != null) {
                onStateChanged();
            } else {
                // Execute frame update
                for (int f = prevState.totalFrame; f < currentState.totalFrame; ++f) {
                    int fr = f % profileState.frames;
                    foreach (var list in currentBehaviours)
                        _FrameUpdateStateBehaviours(list, fr);
                }
            }
        }
    }

    private void DoTriggerTransition(Transition trans) {
        if (trans.actionType == ActionType.ChangeState) {
            State nextState;
            _stateTable.TryGetValue(Animator.StringToHash(trans.targetStateName), out nextState);
            if (nextState != null) {
                if (nextState != currentState.state)
                    Play(trans.targetStateName, trans.priority, (float) trans.targetStateFrame / nextState.frames);
            } else {
                Debug.LogWarning("State " + trans.targetStateName + " doesn't exist, transition will be ignored.");
            }
        } else { // SendMessage
            switch (trans.messageParType) {
                case MessageParType.None:
                    gameObject.SendMessage(trans.messageName, SendMessageOptions.DontRequireReceiver);
                    break;
                case MessageParType.Int:
                    gameObject.SendMessage(trans.messageName, trans.messageParInt, SendMessageOptions.DontRequireReceiver);
                    break;
                case MessageParType.Float:
                    gameObject.SendMessage(trans.messageName, trans.messageParFloat, SendMessageOptions.DontRequireReceiver);
                    break;
                case MessageParType.Bool:
                    gameObject.SendMessage(trans.messageName, trans.messageParBool, SendMessageOptions.DontRequireReceiver);
                    break;
            }
        }

        // Clear triggers
        foreach (var cond in trans.conditions) {
            var param = GetParam(cond.nameIndex);
            if (param.type == ParamType.Trigger)
                param.boolValue = false;
        }
    }


    #endregion

}

}