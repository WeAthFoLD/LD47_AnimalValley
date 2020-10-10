
using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class OverrideStack<T> {

    [ShowInInspector]
    public T value {
        get {
            if (_maxPriority == -1) {
                return _defaultValueEval == null ? _defaultValue : _defaultValueEval.Invoke();
            }

            return _overrideArr[_maxPriority];
        }
    }

    private T _defaultValue;
    private Func<T> _defaultValueEval;

    private T[] _overrideArr = new T[16];
    private BitArray _existArr = new BitArray(16);
    private int _maxPriority = -1;

    public OverrideStack(T defaultValue) {
        _defaultValue = defaultValue;
    }

    public OverrideStack(Func<T> defaultValueEval) {
        _defaultValueEval = defaultValueEval;
    }

    public int Push(T value) {
        for (int i = _maxPriority + 1; i < 16; ++i) {
            if (!_existArr[i]) {
                Push(i, value);
                return i;
            }
        }
        throw new Exception("Stack full!");
    }

    public void Push(int priority, T value) {
        _maxPriority = Mathf.Max(priority, _maxPriority);
        _overrideArr[priority] = value;
        _existArr[priority] = true;
    }

    public void Pop(int priority) {
        _existArr.Set(priority, false);
        if (_maxPriority == priority) {
            for (int i = _maxPriority - 1; i >= 0; --i) {
                if (_existArr[priority]) {
                    _maxPriority = i;
                    return;
                }
            }

            _maxPriority = -1;
        }
    }
}
