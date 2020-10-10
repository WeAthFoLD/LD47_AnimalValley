using UnityEditor;
using UnityEngine;
using EGL = UnityEditor.EditorGUILayout;
using GL = UnityEngine.GUILayout;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System;
using Object = UnityEngine.Object;

public static class EditorGUIUtil {

    public static IntRange FrameRangeSlider(string text, IntRange range, int frames) {
        var prog0 = (float) range.from / frames;
        var prog1 = (float) range.to / frames;
        EGL.MinMaxSlider(text + string.Format(" ({0}-{1})", range.from,range.to), 
            ref prog0, ref prog1, 0, 1);

        return new IntRange(Mathf.RoundToInt(frames * prog0), Mathf.RoundToInt(frames * prog1));
    }

    public static IntRange FrameRangeInput(string text, IntRange range) {
        EGL.BeginHorizontal();
        EGL.PrefixLabel(text);
        range.from = EGL.IntField(range.from);
        EGL.LabelField("->", GL.Width(30));
        range.to = EGL.IntField(range.to);
        EGL.EndHorizontal();
        return range;
    }

    public static string OnGUI(this SearchField self, string label, string content) {
        EGL.BeginHorizontal();
        EGL.PrefixLabel(label);
        content = self.OnGUI(content);
        EGL.EndHorizontal();
        return content;
    }

    public static void AutoCompleteList(string cur, List<string> options, Action<string> onFinishInput, ref Rect popupRect) {
        if (GL.Button(cur, EditorStyles.popup)) {
            PopupWindow.Show(popupRect, new AutoCompletePopup(options, onFinishInput));
        }

        if (Event.current.type == EventType.Repaint) {
            popupRect = GUILayoutUtility.GetLastRect();
        }
    }

    public static T ObjectField<T>(string label, T cur, bool allowSceneObjects = false, params GUILayoutOption[] opts)
        where T: Object {
        return (T) EGL.ObjectField(label, cur, typeof(T), allowSceneObjects, opts);
    }

    class AutoCompletePopup : PopupWindowContent {

        public readonly List<string> options;
        public readonly SearchField searchField;

        readonly AutoCompleteTreeView treeView;

        Action<string> onFinishInput;

        public AutoCompletePopup(List<string> _options, Action<string> _finishInput) {
            options = _options;
            onFinishInput = _finishInput;

            searchField = new SearchField();
            treeView = new AutoCompleteTreeView(this, new TreeViewState());
            treeView.searchString = "";
        }    

        public override void OnGUI(Rect rect) {
            treeView.SetFocus();

            var evt = Event.current;
            if (evt.type == EventType.KeyDown) {
                if (evt.keyCode == KeyCode.Backspace) {
                    evt.Use();
                    if (treeView.searchString.Length > 0) {
                        treeView.searchString = treeView.searchString.Substring(0, treeView.searchString.Length - 1);
                    }
                }

                char ch = evt.character;
                if (!Char.IsControl(ch)) {
                    evt.Use();
                    treeView.searchString += ch;

                    var rows = treeView.GetRows();
                    if (rows.Count > 0) {
                        treeView.SetSelection(new List<int> { rows[0].id });
                    }
                }
            }

            searchField.OnGUI(treeView.searchString);
            treeView.OnGUI(GUILayoutUtility.GetRect(0, 10000, 0, 10000));
        }

        public void ConfirmInput(int id) {
            onFinishInput(options[id]);
            editorWindow.Close();
        }

        public void CancelInput() {
            editorWindow.Close();
        }

    }

    class AutoCompleteTreeView : TreeView {

        readonly AutoCompletePopup parent;

        public AutoCompleteTreeView(AutoCompletePopup _parent, TreeViewState _state) : base(_state) {
            parent = _parent;
            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "root" };
            for (int i = 0; i < parent.options.Count; ++i) {
                var item = new TreeViewItem {
                    id = i,
                    depth = 0,
                    displayName = parent.options[i]
                };
                root.AddChild(item);
            }

            return root;
        }

        public void CallKeyEvent() {
            KeyEvent();
        }

        protected override void KeyEvent() {
            base.KeyEvent();

            var evt = Event.current;
            if (evt.type != EventType.KeyDown)
                return;

            if (evt.keyCode == KeyCode.Return) {
                if (state.selectedIDs.Count > 0) {
                    parent.ConfirmInput(state.selectedIDs[0]);
                }
            }

            if (evt.keyCode == KeyCode.Escape) {
                parent.CancelInput();
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item) {
            return false;
        }

        protected override void DoubleClickedItem(int id) {
            parent.ConfirmInput(id);
        }

    }

}