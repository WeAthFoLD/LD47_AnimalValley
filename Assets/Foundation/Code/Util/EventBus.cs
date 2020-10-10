using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

public sealed class SubscribeEvent : Attribute {

    public int priority = 0;

}

// TODO:
//  - Respect event subclassing?
public static class EventBus {

    // Listen and unlisten
    //   Make a delegate listen to specific event (emit using Post(...)).
    //   Currently doesn't work with event subclassing.

    public static void Listen<T>(Action<T> listener, int priority = 0) {
        ListenInternal(typeof(T), listener, priority);
    }

    public static void Unlisten<T>(Action<T> listener) {
        UnlistenInternal(typeof(T), listener);
    }

    // Attach and detach subscribers
    //   Subscribers are objects whose class contain methods with [SubscribeEvent] attribute.
    //   All methods must have exactly 1 parameter specifying the listening event type.
    //   Attach and Detach adds/removes all the subscribed methods declared in class.

    public static void Attach(object obj) { 
        XDebug.Assert(!_subscriberMethodList.ContainsKey(obj), "Can't attach " + obj + " twice!");

        var listenMethods = GetListenMethods(obj);
        var cacheList = new List<Delegate>();

        foreach (var m in listenMethods) {
            XDebug.Assert(m.method.GetParameters().Length == 1, "Event listener method must have 1 parameter");
            var t = m.method.GetParameters()[0].ParameterType;
            var del = Delegate.CreateDelegate(Expression.GetActionType(t), obj, m.method.Name);
            ListenInternal(t, del, m.priority);
            cacheList.Add(del);
        }

        _subscriberMethodList.Add(obj, cacheList);
    }

    public static void Detach(object obj) { 
        XDebug.Assert(_subscriberMethodList.ContainsKey(obj), obj + " is not attached!");
        
        var listenMethods = GetListenMethods(obj);
        var cacheList = _subscriberMethodList[obj];

        for (int i = 0; i < listenMethods.Length; ++i) {
            var t = listenMethods[i].method.GetParameters()[0].ParameterType;
            UnlistenInternal(t, cacheList[i]);
        }
        
        _subscriberMethodList.Remove(obj);
    }

    // Post an event and let listeners receive it.

    public static void Post<T>(T evt) {
        // XDebug.Log("Event " + evt.GetType() + ": " + evt);

        List<DelegateItem> list;
        if (!_listeners.TryGetValue(typeof(T), out list))
            return;

        // 防重入保护
        _isIteratingCount++;

        foreach (var item in list) {
            // XDebug.Log("Event " + evt.GetType() + " -> " + item.dlg.Method + "/" + item.dlg.Method?.DeclaringType);
            (item.dlg as Action<T>)?.Invoke(evt);
        }

        _isIteratingCount--;

        if (_isIteratingCount == 0) {
            for (var i = 0; i < _waitAddEntries.Count; i++) {
                var entry = _waitAddEntries[i];
                ListenInternal(entry.t, entry.del, entry.priority);
            }

            for (var i = 0; i < _waitRemoveEntries.Count; i++) {
                var entry = _waitRemoveEntries[i];
                UnlistenInternal(entry.t, entry.del);
            }
            _waitAddEntries.Clear();
            _waitRemoveEntries.Clear();
        }
    }


    // --- Implementation

    struct DelegateItem {
        public Delegate dlg;
        public int priority;

        public DelegateItem(Delegate dlg, int priority) {
            this.dlg = dlg;
            this.priority = priority;
        }
    }

    struct ListenMethodItem {
        public MethodInfo method;
        public int priority;
    }

    // System.Type -> Action<?>
    static readonly Dictionary<Type, List<DelegateItem>> _listeners = new Dictionary<Type, List<DelegateItem>>();

    static readonly Dictionary<Type, ListenMethodItem[]> _listenListCache = new Dictionary<Type, ListenMethodItem[]>();

    static readonly Dictionary<object, List<Delegate>> _subscriberMethodList = new Dictionary<object, List<Delegate>>();

    private static int _isIteratingCount = 0;

    private static readonly List<OperationEntry> _waitAddEntries = new List<OperationEntry>();
    private static readonly List<OperationEntry> _waitRemoveEntries = new List<OperationEntry>();

    struct OperationEntry {
        public Type t;
        public Delegate del;
        public int priority;
    }

    static void ListenInternal(Type t, Delegate del, int priority) {
        if (_isIteratingCount > 0) {
            _waitAddEntries.Add(new OperationEntry {
                t = t,
                del = del,
                priority = priority
            });
            return;
        }

        List<DelegateItem> list;
        if (!_listeners.TryGetValue(t, out list)) {
            list = new List<DelegateItem>();
            _listeners.Add(t, list);
        }
        list.Add(new DelegateItem(del, priority));
        list.Sort((lhs, rhs) => -lhs.priority.CompareTo(rhs.priority));

        // XDebug.Log("ListenInternal " + t + "/" + del.Method?.DeclaringType + ":" + del.Method + ", list: " +
        //            string.Join(",", list.Select(x => x.dlg.Method?.DeclaringType + ":" + x.dlg.Method)));
    }

    static void UnlistenInternal(Type t, Delegate del2) {
        if (_isIteratingCount > 0) {
            _waitRemoveEntries.Add(new OperationEntry {
                t = t,
                del = del2
            });
            return;
        }

        if (_listeners.ContainsKey(t)) {
            var dels = _listeners[t];
            dels.RemoveAll(x => x.dlg == del2);
        }
    }

    static ListenMethodItem[] GetListenMethods(object obj) {
        var type = obj.GetType();
        if (_listenListCache.ContainsKey(type)) {
            return _listenListCache[type];
        } else {
            var ret = obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(it => it.GetCustomAttribute<SubscribeEvent>() != null)
                .Select(it => new ListenMethodItem { method = it, priority = it.GetCustomAttribute<SubscribeEvent>().priority })
                .ToArray();
            _listenListCache.Add(type, ret);
            return ret;
        }
    }

}
