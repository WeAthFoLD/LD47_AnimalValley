using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
    
public class MonoTimeManager : MonoBehaviour, ISingleton {
    public class TimeAffectInfo {
        public float scale;
        public float time;
        public bool hasTime;
    }
    
    readonly Dictionary<object, TimeAffectInfo> activeInfo = new Dictionary<object, TimeAffectInfo>();
    private readonly List<object> _toRemove = new List<object>();
    
    public bool paused;

    [ShowInInspector, HorizontalGroup("GT")]
    [Range(0, 2f)]
    float _globalTimeScale = 1f;

    [HorizontalGroup("GT"), Button("Rst", ButtonSizes.Small)]
    void ResetGlobalTS() {
        _globalTimeScale = 1f;
    }

    public void AddTimeAffect(object id, float scale, float time = -1f) {
        AddTimeAffect(id, new TimeAffectInfo { scale = scale, time = time, hasTime = time > 0 });
    }
    
    public void AddTimeAffect(object id, TimeAffectInfo info) {
        activeInfo.Remove(id);
        activeInfo.Add(id, info);
    }
    
    public void ClearTimeAffect() {
        activeInfo.Clear();
    }
    
    public void RemoveTimeAffect(object id) {
        activeInfo.Remove(id);
    }
    
    void Update() {
        if (paused) {
            Time.timeScale = 0.0f;
        } else {
            var undt = Time.unscaledDeltaTime;
            var scale = _globalTimeScale;

            using (var enumerator = activeInfo.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    var entry = enumerator.Current;
                    var info = entry.Value;
                    info.time -= undt;
                    if (info.hasTime && info.time <= 0) {
                        _toRemove.Add(entry.Key);
                    } else {
                        scale *= info.scale;
                    }
                }
            }

            for (int i = 0; i < _toRemove.Count; ++i) {
                activeInfo.Remove(_toRemove[i]);
            }

            _toRemove.Clear();

            if (scale != Time.timeScale) {
                Time.timeScale = scale;
                Time.fixedDeltaTime = 0.02f * scale;
            }
        }
    }


    public void Init(MonoGameManager manager) {
    }
}

