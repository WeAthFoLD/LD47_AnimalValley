using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AnimatorTransition {

[Serializable]
public class StatePortion {
    public string name;
    public IntRange range;
    public bool includeEnd;
    public string tags;

    public List<StateBehaviour> behaviours = new List<StateBehaviour>();

    public int nameHash {
        get {
            if (_nameHash == 0)
                _nameHash = Animator.StringToHash(name);
            return _nameHash;
        }
    }

    private int _nameHash;

    private int[] _tagCache;

    public void ClearCache() {
        _tagCache = null;
    }

    public bool HasTag(int tagHash) {
        if (_tagCache == null) {
            var tagArray = tags.Split(',');
            _tagCache = new int[tagArray.Length];
            for (var i = 0; i < tagArray.Length; i++) {
                var tag = tagArray[i];
                _tagCache[i] = Animator.StringToHash(tag);
            }
        }

        for (int i = 0; i < _tagCache.Length; ++i)
            if (tagHash == _tagCache[i])
                return true;
        return false;
    }

    public bool IsInPortion(int frame) {
        return range.from <= frame && (includeEnd || frame <= range.to);
    }

}

[Serializable]
public class State : ScriptableObject {

    private static int _fullNameHash = Animator.StringToHash("full");

    public TransitionProfile profile;
    public string stateName = "";
    public int frames;
    public string tags = "";
    public List<StatePortion> portions = new List<StatePortion>();

    public List<StateBehaviour> behaviours = new List<StateBehaviour>();

    private int[] _tagCache;

    public bool SelfHasTag(int tagHash) {
        if (_tagCache == null) {
            var tagArray = tags.Split(',');
            _tagCache = new int[tagArray.Length];
            for (var i = 0; i < tagArray.Length; i++) {
                var tag = tagArray[i];
                _tagCache[i] = Animator.StringToHash(tag);
            }
        }

        for (int i = 0; i < _tagCache.Length; ++i)
            if (tagHash == _tagCache[i])
                return true;
        return false;
    }

    private StatePortion _full;
    
    public StatePortion full {
        get {
            if (_full == null) {
                _full = new StatePortion {
                    name = "full",
                    range = new IntRange(0, int.MaxValue),
                    tags = tags,
                    includeEnd = true
                };
            }

            return _full;
        }
    }

    private List<StatePortion> _allPortions;

    public List<StatePortion> allPortions {
        get {
            if (_allPortions == null) {
                _allPortions = new List<StatePortion> {full};
                _allPortions.AddRange(portions);
            }
            return _allPortions;
        }
    }

    public StatePortion FindPortion(int nameHash) {
        if (nameHash == _fullNameHash) {
            return full;
        }
        foreach (var p in portions) {
            if (p.nameHash == nameHash)
                return p;
        }
        return null;
    }

    public bool IsPortion(int portionHash, int frame) {
        var p = FindPortion(portionHash);
        return p != null && p.IsInPortion(frame);
    }

    public bool HasTag(int frame, int tagHash) {
        if (SelfHasTag(tagHash)) {
            return true;
        }

        foreach (var p in portions) {
            if (p.IsInPortion(frame) && p.HasTag(tagHash)) {
                return true;
            }
        }
        
        return false;
    }

    public bool HasTagAnyFrame(int tagHash) {
        _tagCache = null;
        if (SelfHasTag(tagHash)) {
            return true;
        }
        foreach (var p in portions) {
            p.ClearCache();
            if (p.HasTag(tagHash)) {
                return true;
            }
        }
        return false;
    }
}

}