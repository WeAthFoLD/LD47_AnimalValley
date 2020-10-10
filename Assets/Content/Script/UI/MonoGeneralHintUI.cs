using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralHintEvent {
    public string msg;
    public float? overrideDuration;
    public bool forceOverride = false;
    public int layer;
}

public class MonoGeneralHintUI : MonoListenEventBehaviour{
    public float keepTime = 2.0f;
    public int layer = 0;

    private float remain;
    private Text text;

    private Queue<GeneralHintEvent> _msgQueue = new Queue<GeneralHintEvent>();

    void Start() {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update() {
        remain -= Time.deltaTime;
        if (remain <= 0f) {
            text.text = "";
        }

        if (string.IsNullOrEmpty(text.text) && _msgQueue.Count > 0) {
            OnGeneralHint(_msgQueue.Dequeue());
        }
    }

    [SubscribeEvent]
    private void OnGeneralHint(GeneralHintEvent ev) {
        if (ev.layer != layer)
            return;

        if (string.IsNullOrEmpty(text.text) || ev.forceOverride) {
            remain = ev.overrideDuration ?? keepTime;
            text.text = ev.msg;

            _msgQueue.Clear();
        } else {
            _msgQueue.Enqueue(ev);
        }
    }
}
