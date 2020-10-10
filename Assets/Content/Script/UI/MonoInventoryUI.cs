using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using SPlay;
using UnityEngine;
using UnityEngine.UI;

public class MonoInventoryUI : MonoBehaviour {
    private const string CTRL_KEY = "Inventory";

    public GameObject itemPrefab;

    public Transform contentRoot;

    public GameObject emptyText;

    public Button closeBtn;

    private MonoPlayer _owner;

    private StudioEventEmitter _emitter;

    private float _timer;

    public void SetupView(MonoPlayer player) {
        _owner = player;
        _emitter = GetComponent<StudioEventEmitter>();
        RefreshView();

        _owner.disableControlCount.Add(CTRL_KEY);

        closeBtn.BindViewCallback(Close);
    }

    private void Update() {
        // 目前有些意外情况会导致状态改变，所以……
        _timer += Time.deltaTime;
        if (_timer > 1.25f) {
            RefreshView();
            _timer = 0f;
        }
    }

    private void RefreshView() {
        foreach (Transform child in contentRoot) {
            Destroy(child.gameObject);
        }

        emptyText.SetActive(_owner.inventory.items.Count == 0);

        for (var i = 0; i < _owner.inventory.items.Count; i++) {
            var stack = _owner.inventory.items[i];
            var instance = Instantiate(itemPrefab, contentRoot);
            var index = i;
            instance.GetComponent<MonoInventoryItem>().SetupView(stack, () => {
                var ctx = new ItemUseContext {_owner = _owner};
                stack.item.onUseExpr?.Evaluate(ctx);

                if (!string.IsNullOrEmpty(stack.item.clickSound)) {
                    _emitter.Event = stack.item.clickSound;
                    _emitter.Lookup();
                    _emitter.Play();
                }

                stack.number -= 1;
                if (stack.number == 0) {
                    _owner.inventory.items.RemoveAt(index);
                }
                RefreshView();
            });
        }
    }

    private void Close() {
        gameObject.SetActive(false);
        _owner.disableControlCount.Remove(CTRL_KEY);
    }

    class ItemUseContext : ExpressionContext {
        private static StringHash OWNER = new StringHash("Owner");

        public MonoPlayer _owner;

        public object GetVariable(StringHash nameHash) {
            if (nameHash == OWNER) {
                return _owner;
            }

            return null;
        }
    }

}
