using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoBindingPoints : MonoBehaviour
{
    [Serializable]
    public struct Entry {
        public string name;
        public Transform transform;
    }

    public List<Entry> bindingPoints = new List<Entry>();

    public void Add(string name, Transform trans) {
        bindingPoints.Add(new Entry {
            name = name,
            transform = trans
        });
    }

    public Transform Find(string name) {
        for (var i = 0; i < bindingPoints.Count; i++) {
            var item = bindingPoints[i];
            if (item.name == name)
                return item.transform;
        }

        return null;
    }
}
