using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lazy<T> {

    private T _val;
    private bool _initialized;
    private Func<T> _creationFunc;

    public T val {
        get {
            if (!_initialized) {
                _initialized = true;
                _val = _creationFunc();
                _creationFunc = null;
            }

            return _val;
        }
    }

    public Lazy(Func<T> creationFunc) {
        _creationFunc = creationFunc;
    }
}
