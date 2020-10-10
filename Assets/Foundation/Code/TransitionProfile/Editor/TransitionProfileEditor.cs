using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

using EGL = UnityEditor.EditorGUILayout;
using GL = UnityEngine.GUILayout;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;

using AnimationReflection = AnimationNavigator.ReflectionInterface;

namespace AnimatorTransition {

internal static class Utils {

    public static AnimatorState FindState(AnimatorStateMachine machine, string name) {
        foreach (var st in machine.states) {
            if (st.state.name == name)
                return st.state;
        }
        foreach (var m in machine.stateMachines) {
            var st = FindState(m.stateMachine, name);
            if (st != null)
                return st;
        }
        return null;
    }

    public static void FocusEditingAnimation(TransitionProfile profile, string stateName) {
        var target = AnimationReflection.inst.GetEditTarget();
        if (target != null && target.animator && profile.controller == target.animator.runtimeAnimatorController) {
            var clip = Utils.FindState(profile.controller.layers[0].stateMachine, stateName)?.motion as AnimationClip;
            if (clip)
                AnimationReflection.inst.ChangeSelection(clip);
            else
                Debug.LogWarning("No corresponding animation clip for state " + stateName);
        }
    }
}

public class TransitionProfileEditor : EditorWindow {

    enum GlobalPanelType {
        States, Parameters, Tags, Transitions
    }

    [SerializeField]
    TransitionProfile profile;

    GlobalPanelType globalPanel = GlobalPanelType.States;

    SearchField searchFieldState, searchFieldParam, searchFieldTag, searchFieldFromState, searchFieldTargetState;

    [SerializeField]
    TreeViewState statePanelState, paramPanelState, tagsPanelState, transitionPanelState;

    TreeView statePanelView, paramPanelView, tagsPanelView;

    TransitionTreeView transitionPanelView;

    public void BeginEdit(TransitionProfile profile) {
        statePanelView = paramPanelView = tagsPanelView = null;
        transitionPanelView = null;
        OnEnable();

        this.profile = profile;
    }

    void OnEnable() {
        searchFieldState = new SearchField();
        searchFieldParam = new SearchField();
        searchFieldFromState = new SearchField();
        searchFieldTag = new SearchField();
        searchFieldTargetState = new SearchField();

        statePanelState = new TreeViewState();
        paramPanelState = new TreeViewState();
        tagsPanelState = new TreeViewState();
        transitionPanelState = new TreeViewState();
    }

    string ToolbarSearchField(string str, SearchField field) {
        EGL.BeginHorizontal(EditorStyles.toolbar);
        GL.Label("", GL.Width(150));
        str = field.OnToolbarGUI(str);
        EGL.EndHorizontal();
        return str;
    }

    void OnGUI() {
        if (!profile)
            return;

        if (Selection.activeObject != profile && Selection.activeObject is TransitionProfile) {
            BeginEdit(Selection.activeObject as TransitionProfile);
        }

        globalPanel = (GlobalPanelType) GL.Toolbar((int) globalPanel, new string[] { "States", "Params", "Tags", "Transitions" }, EditorStyles.toolbarButton);

        switch (globalPanel) {
            case GlobalPanelType.States: {
                profile.controller = (AnimatorController) EGL.ObjectField("Controller", profile.controller, typeof(AnimatorController), allowSceneObjects: false);

                if (profile.controller) {
                    EGL.BeginHorizontal();
                    EGL.PrefixLabel("Layer");
                    profile.controllerLayer = EGL.IntField(profile.controllerLayer);

                    // GL.FlexibleSpace();
                    if (GL.Button("Sync", EditorStyles.miniButtonRight, GL.Width(40)) && profile.controller) {
                        SyncControllerStates();
                        statePanelView.Reload();
                    }
                    EGL.EndHorizontal();
                }
                EGL.Space();

                if (statePanelView == null) {
                    statePanelView = new StatesTreeView(statePanelState, profile);
                    statePanelView.searchString = "";

                    searchFieldState.downOrUpArrowKeyPressed += statePanelView.SetFocusAndEnsureSelectedItem;
                }
                statePanelView.searchString = ToolbarSearchField(statePanelView.searchString, searchFieldState);
                statePanelView.OnGUI(GUILayoutUtility.GetRect(0, 10000, 0, 10000));

            } break;
            case GlobalPanelType.Parameters: {
                if (paramPanelView == null) {
                    paramPanelView = new ParamsTreeView(paramPanelState, profile);
                    paramPanelView.searchString = "";
                }

                paramPanelView.searchString = ToolbarSearchField(paramPanelView.searchString, searchFieldParam);
                
                paramPanelView.OnGUI(GUILayoutUtility.GetRect(0, 10000, 0, 10000));

            } break;
            case GlobalPanelType.Tags: {
                if (tagsPanelView == null) {
                    tagsPanelView = new TagsTreeView(tagsPanelState, profile);
                }
                
                tagsPanelView.searchString = ToolbarSearchField(tagsPanelView.searchString, searchFieldTag);

                tagsPanelView.OnGUI(GUILayoutUtility.GetRect(0, 10000, 0, 10000));

                var selection = tagsPanelView.GetSelection();
                if (selection.Any()) {
                    string selectedTag = profile.tags[selection.First()];
                    var relatedStates = profile.states.Where(state => state.HasTagAnyFrame(Animator.StringToHash(selectedTag))).Select(it => it.stateName);
                    
                    string statesStr = string.Join(",", relatedStates.ToArray());
                    GL.Label(statesStr, EditorStyles.textArea);
                }

            } break;
            default: { // Transitions
                if (transitionPanelView == null) {
                    transitionPanelView = new TransitionTreeView(transitionPanelState, profile);
                }

                EditorGUI.BeginChangeCheck();
                transitionPanelView.fromStateFilter = searchFieldFromState.OnGUI("From State", transitionPanelView.fromStateFilter);
                transitionPanelView.targetStateFilter = searchFieldTargetState.OnGUI("Target State", transitionPanelView.targetStateFilter);
                if (EditorGUI.EndChangeCheck()) {
                    transitionPanelView.Reload();
                }
                EGL.Space();

                transitionPanelView.OnGUI(GUILayoutUtility.GetRect(0, 10000, 0, 10000));

                Repaint();
            } break;
        }
    }

    void SyncControllerStates() {
        // Clear previous states
        var layer = profile.controller.layers[profile.controllerLayer];

        var list = new List<State>();
        SyncControllerStates_Impl(list, layer.stateMachine);

        for (int i = 0; i < list.Count; ++i) {
            var st = list[i];
            var original = profile.FindState(st.stateName);

            if (original != null) {
                list[i] = original;
                list[i].frames = st.frames;
            }
        }

        // Add states as asset to profile
        foreach (var state in list) {
            state.hideFlags = HideFlags.HideInHierarchy;
            if (!profile.states.Contains(state))
                AssetDatabase.AddObjectToAsset(state, profile);
        }

        foreach (var state in profile.states) {
            if (!list.Contains(state)) {
                DestroyImmediate(state, true);
            }
        }
        profile.states = list;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    void SyncControllerStates_Impl(List<State> states, AnimatorStateMachine machine) {
        foreach (var st in machine.states) {
            var state = (State) ScriptableObject.CreateInstance(typeof(State));
            state.name = "State " + st.state.name;
            state.stateName = st.state.name;
            state.profile = profile;

            var clip = (AnimationClip) st.state.motion;
            var frames = clip == null ? 1 : clip.length / st.state.speed;
            state.frames = FrameUtil.Time2Frame(frames);
            
            states.Add(state);
        }

        foreach (var m in machine.stateMachines) {
            SyncControllerStates_Impl(states, m.stateMachine);
        }
    }

    class StatesTreeView : TreeView {

        readonly TransitionProfile profile;

        public StatesTreeView(TreeViewState state, TransitionProfile profile) : base(state) {
            this.profile = profile;
            showAlternatingRowBackgrounds = true;
            
            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = 0, depth = -1 };
            var rows = new List<TreeViewItem>();
            foreach (var state in profile.states) {
                var item = new TreeViewItem(state.GetInstanceID(), 0, state.stateName);
                rows.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, rows);
            
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item) {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds) {
            if (selectedIds.Count > 0) {
                Selection.activeObject = profile.states.Find(it => it.GetInstanceID() == selectedIds[0]);

                Utils.FocusEditingAnimation(profile, ((State) Selection.activeObject).stateName);
            }
        }

    }

    class ParamsTreeView : TreeView {

        public static MultiColumnHeaderState CreateColumnHeaderState() {
            var columns = new [] {
                new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("Name"),
                    width = 150,
                    minWidth = 100,
                    autoResize = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("Type"),
                    autoResize = false,
                    width = 50,
                    minWidth = 50,
                    maxWidth = 50,
                    canSort = false
                },
                new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("Value"),
                    autoResize = true,
                    minWidth = 100,
                    maxWidth = 200,
                    canSort = false
                }
            };
            return new MultiColumnHeaderState(columns);
        }

        readonly TransitionProfile profile;

        public ParamsTreeView(TreeViewState state, TransitionProfile profile)
            : base(state, new MultiColumnHeader(CreateColumnHeaderState())) {
            this.profile = profile;
            this.multiColumnHeader.height = MultiColumnHeader.DefaultGUI.minimumHeight;
            this.rowHeight *= 1.1f;
            showAlternatingRowBackgrounds = true;

            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = -1, depth = -1 };
            var rows = new List<TreeViewItem>();

            int idx = 0;
            foreach (var param in profile.parameters) {
                var item = new TreeViewItem(idx, 0, param.name);
                rows.Add(item);

                ++idx;
            }

            SetupParentsAndChildrenFromDepths(root, rows);
            
            return root;
        }

        protected override void RowGUI(RowGUIArgs args) {
            var item = args.item;
            var param = profile.parameters[item.id];

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i) {
                var cellRect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref cellRect);

                switch (args.GetColumn(i)) {
                    case 0: { // name
                        // param.name = EditorGUI.TextField(cellRect, param.name);
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                    } break;
                    case 1: { // type
                        param.type = (ParamType) EditorGUI.EnumPopup(cellRect, param.type, EditorStyles.toolbarPopup);
                    } break;
                    case 2: { // value
                        switch (param.type) {
                            case ParamType.Trigger:
                            case ParamType.Bool: {
                                param.boolValue = EditorGUI.Toggle(cellRect, param.boolValue);
                            } break;
                            case ParamType.Float: {
                                param.floatValue = EditorGUI.FloatField(cellRect, param.floatValue);
                            } break;
                            case ParamType.Int: {
                                param.intValue = EditorGUI.IntField(cellRect, param.intValue);
                            } break;
                        }
                    } break;
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(profile);
            }
        }

        protected override bool CanRename(TreeViewItem item) {
            return true;
        }

        protected override void RenameEnded(RenameEndedArgs args) {
            if (args.acceptedRename) {
                var param = profile.parameters[args.itemID];
                param.name = args.newName;
                EditorUtility.SetDirty(profile);                

                Reload();
            }
        }

        protected override Rect GetRenameRect (Rect rowRect, int row, TreeViewItem item) {
			Rect cellRect = GetCellRectForTreeFoldouts (rowRect);
			CenterRectUsingSingleLineHeight(ref cellRect);
			return base.GetRenameRect (cellRect, row, item);
		}

        protected override void ContextClickedItem(int itemID) {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add"), false, OnAddParam, itemID);
            menu.AddItem(new GUIContent("Delete"), false, OnRemoveParam, itemID);

            menu.ShowAsContext();

            Event.current.Use();
        }

        protected override void ContextClicked() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add"), false, OnAddParam, null);
            menu.ShowAsContext();            
        }

        void OnRemoveParam(object _itemID) {
            int itemID = (int) _itemID;
            profile.parameters.RemoveAt(itemID);
            EditorUtility.SetDirty(profile);

            Reload();
        }

        void OnAddParam(object _itemID) {
            int itemID = (int) (_itemID ?? profile.parameters.Count);
            profile.parameters.Insert(itemID, new Param());
            EditorUtility.SetDirty(profile);

            Reload();
            BeginRename(GetRows()[itemID]);
        }

    }

    public class TagsTreeView : TreeView {

        TransitionProfile profile;

        public TagsTreeView(TreeViewState state, TransitionProfile profile)
            : base(state) {
            this.profile = profile;       
            showAlternatingRowBackgrounds = true;            
            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = -1, depth = -1 };
            root.children = new List<TreeViewItem>();
            for (int i = 0; i < profile.tags.Count; ++i) {
                root.AddChild(new TreeViewItem(i, 0, profile.tags[i]));
            }
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override bool CanRename(TreeViewItem item) {
            return true;
        }

        protected override void RenameEnded(RenameEndedArgs args) {
            if (args.acceptedRename) {
                profile.tags[args.itemID] = args.newName;
                EditorUtility.SetDirty(profile);

                Reload();
            }
        }

        protected override void ContextClickedItem(int itemID) {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add"), false, OnAddTag, itemID);            
            menu.AddItem(new GUIContent("Delete"), false, OnRemoveTag, itemID);

            menu.ShowAsContext();

            Event.current.Use();
        }

        protected override void ContextClicked() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add"), false, OnAddTag, null);
            menu.ShowAsContext();        
            Event.current.Use();                
        }

        void OnRemoveTag(object _itemID) {
            int itemID = (int) _itemID;
            profile.tags.RemoveAt(itemID);
            Reload();
            EditorUtility.SetDirty(profile);
        }

        void OnAddTag(object _itemID) {
            int itemID = (int) (_itemID ?? profile.tags.Count);
            profile.tags.Insert(itemID, "");
            EditorUtility.SetDirty(profile);
            Reload();

            BeginRename(this.GetRows()[itemID]);
        }

    }

    public class TransitionTreeView : TreeView {

        static MultiColumnHeaderState CreateColumnHeaderState() {
            var columns = new[] {
                new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("On"),
                    width = 25,
                    minWidth = 50,
                    maxWidth = 50,
                    canSort = false
                },
                new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("From"),
                    width = 150,
                    minWidth = 100,
                    maxWidth = 200,
                    canSort = false
                },
                new MultiColumnHeaderState.Column {
                    headerContent = new GUIContent("To"),
                    width = 150,                    
                    minWidth = 100,
                    maxWidth = 200,
                    canSort = false
                },
            };
            return new MultiColumnHeaderState(columns);
        }

        public string fromStateFilter = "", targetStateFilter = "";

        readonly TransitionProfile profile;

        public TransitionTreeView(TreeViewState state, TransitionProfile profile)
            : base(state, new MultiColumnHeader(CreateColumnHeaderState()) ) {
            this.profile = profile;
            multiColumnHeader.height = MultiColumnHeader.DefaultGUI.minimumHeight;
            showAlternatingRowBackgrounds = true;

            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = -1, depth = -1 };
            var rows = new List<TreeViewItem>();

            for (int i = 0; i < profile.transitions.Count; ++i) {
                var transition = profile.transitions[i];
                if (transition.fromState.GetStatePreview().Contains(fromStateFilter) &&
                    transition.targetInfo.Contains(targetStateFilter)) {
                    rows.Add(new TreeViewItem { id = i, depth = 0 });
                }
            }

            SetupParentsAndChildrenFromDepths(root, rows);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args) {
            var item = args.item;
            var transition = profile.transitions[item.id];

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i) {
                var cellRect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref cellRect);

                switch (args.GetColumn(i)) {
                    case 0: { // on
                        transition.enabled = EditorGUI.Toggle(cellRect, transition.enabled);
                    } break;
                    case 1: { // from
                        EditorGUI.LabelField(cellRect, transition.fromState.GetStatePreview());
                    } break;
                    case 2: { // to
                        EditorGUI.LabelField(cellRect, transition.targetInfo);
                    } break;
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(profile);
            }
        }

        protected override void ContextClickedItem(int itemID) {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add"), false, OnAddTransition, itemID);
            menu.AddItem(new GUIContent("Delete"), false, OnRemoveTransition, itemID);
            menu.AddItem(new GUIContent("Duplicate"), false, OnDuplicateTransition, itemID);

            menu.ShowAsContext();

            Event.current.Use();
        }

        void OnRemoveTransition(object _itemID) {
            int itemID = (int) _itemID;

            profile.transitions.RemoveAt(itemID);
            Reload();
        }

        void OnAddTransition(object _itemID) {
            int itemID = _itemID == null ? profile.transitions.Count : (int) _itemID;

            var transition = (Transition) ScriptableObject.CreateInstance(typeof(Transition));
            transition.profile = profile;
            transition.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(transition, profile);
            profile.transitions.Insert(itemID, transition);
            Selection.activeObject = transition;
            Reload();
        }

        void OnDuplicateTransition(object _itemID) {
            int itemID = (int) _itemID;
            
            var transition = (Transition) Instantiate(profile.transitions[itemID]);
            transition.hideFlags = HideFlags.HideInHierarchy;            
            AssetDatabase.AddObjectToAsset(transition, profile);
            profile.transitions.Insert(itemID, transition);
            Selection.activeObject = transition;
            Reload();    
        }

        protected override void ContextClicked() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add"), false, OnAddTransition, profile.transitions.Count);
            menu.ShowAsContext();
        }

        protected override void SelectionChanged(IList<int> selectedIds) {
            if (selectedIds.Count > 0) {
                Selection.activeObject = profile.transitions[selectedIds[0]];   
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args) {
            return true;
        }

        protected override bool CanMultiSelect(TreeViewItem item) {
            return false;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args) {
            DragAndDrop.PrepareStartDrag();

            var transitions = args.draggedItemIDs.Select(it => (UnityEngine.Object) profile.transitions[it]).ToArray();
            DragAndDrop.objectReferences = transitions;
            DragAndDrop.StartDrag("Transition");

            Event.current.Use();
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args) {
            if (DragAndDrop.objectReferences.Length != 1 ||
                !(DragAndDrop.objectReferences[0] is Transition)) {
                return DragAndDropVisualMode.Rejected;
            }

            var dragging = DragAndDrop.objectReferences[0] as Transition; 
            
            if (args.performDrop) {
                int insertAtIndex = 0;
                switch (args.dragAndDropPosition) {
                    case DragAndDropPosition.BetweenItems: 
                        insertAtIndex = args.insertAtIndex; 
                        break;
                    case DragAndDropPosition.UponItem:
                        insertAtIndex = args.parentItem.id;
                        break;
                    case DragAndDropPosition.OutsideItems:
                        insertAtIndex = profile.transitions.Count;
                        break;
                }
                insertAtIndex = Mathf.Max(0, insertAtIndex);

                var prevIndex = profile.transitions.IndexOf(dragging);
                profile.transitions.RemoveAt(prevIndex);                
                if (prevIndex < insertAtIndex) {
                    profile.transitions.Insert(insertAtIndex - 1, dragging);                    
                } else {
                    profile.transitions.Insert(insertAtIndex, dragging);
                }

                Reload();
            }
            return DragAndDropVisualMode.Move;            
        }

    }

}

[CustomEditor(typeof(TransitionProfile))]
public class TransionProfileInspector : Editor {
    
    public override void OnInspectorGUI() {
        GUI.enabled = false;
        EGL.ObjectField("Controller", ((TransitionProfile) target).controller, 
                        typeof(AnimatorController), false);
        GUI.enabled = true;

        if (GUILayout.Button("Edit", GUILayout.MaxWidth(150))) {
            var instance = EditorWindow.GetWindow<TransitionProfileEditor>();
            instance.titleContent = new GUIContent("Transition Prof.");

            instance.BeginEdit((TransitionProfile) target);
        }
    }

}

[CustomEditor(typeof(Transition))]
public class TransitionInspector : Editor {
    List<string> allStateNames;

    Transition transition;

    Rect fromStateRect;

    Rect targetStateRect;

    Editor fromStateEditor;
    
    void OnEnable() {
        transition = (Transition) target;
        
        allStateNames = transition.profile.states
                            .ConvertAll(state => state.stateName);
    }

    public override void OnInspectorGUI() {
        transition = (Transition) target;

        EditorGUI.BeginChangeCheck();

        var fromName = transition.fromState.GetStatePreview();
        var toName = transition.targetInfo;
        var displayName = fromName + "->" + toName;
        EGL.LabelField(displayName);
        EGL.Space();

        FromStateFilterInspector(transition.profile, transition.fromState, ref fromStateRect);
        EGL.Space();
        
        transition.triggerRangeType = (TriggerRangeType) EGL.EnumPopup("Trigger Range Type", transition.triggerRangeType);

        if (transition.triggerRangeType == TriggerRangeType.Range)
            transition.triggerRange = EditorGUIUtil.FrameRangeInput("Trigger Frame", transition.triggerRange);
        if (transition.triggerRangeType == TriggerRangeType.FrameSinceExec || transition.triggerRangeType == TriggerRangeType.FrameSinceExecBefore)
            transition.triggerFrameSinceExec = EGL.IntField("Frame Since Exec", transition.triggerFrameSinceExec);
        
        transition.timeBuffer = EGL.FloatField("Time Buffer", transition.timeBuffer);

        using (new EGL.VerticalScope(EditorStyles.helpBox))  {
            var conds = transition.conditions;
            var paramNames = transition.profile.parameters.Select(it => it.name).ToArray();

            using (new EGL.HorizontalScope()) {
                EGL.LabelField("Conditions", EditorStyles.boldLabel);
                GL.FlexibleSpace();
                if (GL.Button("+", GL.Width(30))) {
                    conds.Add(new Condition());
                }
            }
            for (int i = 0; i < conds.Count; ++i) {
                var cond = conds[i];
    
                EGL.BeginHorizontal();
    
                int condSelectIndex = Mathf.Max(0, Array.IndexOf(paramNames, cond.name));
    
                // cond.name = EGL.TextField(cond.name, GL.Width(70));
                condSelectIndex = EGL.Popup(condSelectIndex, paramNames);
                cond.name = paramNames[condSelectIndex];

                var param = transition.profile.FindParam(cond.name);
                if (param == null) {
                    EGL.LabelField("!Doesn't exist");
                } else {
                    var type = param.type;
                    if (type == ParamType.Bool) {
                        cond.boolValue = EGL.Toggle(cond.boolValue);
                    }
                    else if (type != ParamType.Trigger) { // Trigger 不需要编辑
                        cond.cmp = (Cmp) EGL.EnumPopup(cond.cmp, GL.Width(50));
    
                        if (type == ParamType.Int) {
                            cond.intValue = EGL.IntField(cond.intValue);
                        } else {
                            cond.floatValue = EGL.FloatField(cond.floatValue);
                        }
                    }
                }
                
                GL.FlexibleSpace();
                if (GL.Button("-", GL.Width(30))) {
                    conds.RemoveAt(i);
                    --i;
                }
    
                EGL.EndHorizontal();
            }
        }
        
        EGL.LabelField("", GUI.skin.horizontalSlider);

        transition.actionType = (ActionType) EGL.EnumPopup("Action", transition.actionType);

        if (transition.actionType == ActionType.ChangeState) {
            EGL.BeginHorizontal();
            EGL.PrefixLabel("Target State");
            EditorGUIUtil.AutoCompleteList(transition.targetStateName, allStateNames,
                str => transition.targetStateName = str, ref targetStateRect);

            transition.targetStateFrame = EGL.IntField(transition.targetStateFrame, GL.Width(30));
            EGL.LabelField("F", GUILayout.Width(20));

            var targetState = transition.profile.FindState(transition.targetStateName);
            if (targetState) {
                if (GL.Button("Focus")) {
                    Utils.FocusEditingAnimation(transition.profile, targetState.stateName);
                }
            }
            EGL.EndHorizontal();

            if (!targetState) {
                EGL.HelpBox("No target state " + targetState, MessageType.Error);
            } 
        } else { // SendMessage
            transition.messageName = EGL.TextField("Message Name", transition.messageName);

            EGL.Space();
            transition.messageParType = (MessageParType) EGL.EnumPopup("Parameter Type", transition.messageParType);
            
            switch (transition.messageParType) {
                case MessageParType.Int:
                    transition.messageParInt = EGL.IntField("Value", transition.messageParInt);
                    break;
                case MessageParType.Float:
                    transition.messageParFloat = EGL.FloatField("Value", transition.messageParFloat);
                    break;
                case MessageParType.Bool:
                    transition.messageParBool = EGL.Toggle("Value", transition.messageParBool);
                    break;
            }
        }

        transition.priority = EGL.IntField("Priority", transition.priority);
        transition.shouldDelay = EGL.Toggle("Should Delay", transition.shouldDelay);
        if (transition.shouldDelay) {
            transition.delay = EGL.FloatField("Delay", transition.delay);
        }

        if (EditorGUI.EndChangeCheck()) {
            EditorUtility.SetDirty(transition);
        }

        if (transition.fromState.type == FromStateType.State) {
            EGL.LabelField("", GUI.skin.horizontalSlider);
            using (new EGL.VerticalScope(EditorStyles.helpBox)) {
                EGL.LabelField("From State", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                var fromState = transition.profile.FindState(transition.fromState.stateOrTagName);
                if (fromState) {
                    GUI.enabled = false;
                    if (!fromStateEditor || fromStateEditor.target != fromState) {
                        if (fromStateEditor) DestroyImmediate(fromStateEditor);
                        fromStateEditor = Editor.CreateEditor(fromState);
                    }

                    fromStateEditor.OnInspectorGUI();
                    GUI.enabled = true;
                }
                --EditorGUI.indentLevel;
            }
        }
    }


    void FromStateFilterInspector(TransitionProfile profile, FromStateFilter filter, ref Rect stateRect) {
        EGL.BeginHorizontal();
        filter.type = (FromStateType) EGL.EnumPopup(filter.type, GL.Width(70));

        if (filter.type == FromStateType.State) {
            EditorGUIUtil.AutoCompleteList(filter.stateOrTagName, allStateNames, str => filter.stateOrTagName = str, ref stateRect);
        } else if (filter.type == FromStateType.Tag) {
            EditorGUIUtil.AutoCompleteList(filter.stateOrTagName, transition.profile.tags, str => filter.stateOrTagName = str, ref stateRect);
        }

        if (filter.type == FromStateType.State) {
            var state = transition.profile.FindState(filter.stateOrTagName);
            if (state == null) {
                EGL.EndHorizontal();
                EGL.HelpBox("No Source State", MessageType.Error);
            } else {
                List<string> portionSelections = new List<string>();
                portionSelections.Add("<any>");
                foreach (var p in state.allPortions) {
                    portionSelections.Add(p.name);
                }

                var prevIndex = portionSelections.IndexOf(filter.portionName);
                if (prevIndex == -1)
                    prevIndex = 0;
                
                prevIndex = EGL.Popup(prevIndex, portionSelections.ToArray());
                filter.portionName = prevIndex == 0 ? "" : portionSelections[prevIndex];

                if (GL.Button("Focus")) {
                    Utils.FocusEditingAnimation(profile, state.stateName);
                }
                EGL.EndHorizontal();
            }
        } else {
            EGL.EndHorizontal();
        }
    }

}

[CustomEditor(typeof(State))]
public class StateInspector : Editor {

    State state;

    string addStatePortionName = "";
    
    HashSet<StatePortion> activePortions = new HashSet<StatePortion>();

    HashSet<StateBehaviour> activeBehaviours = new HashSet<StateBehaviour>();

    Dictionary<StateBehaviour, Editor> behaviourEditors = new Dictionary<StateBehaviour, Editor>();

    Dictionary<int, Rect> popupRects = new Dictionary<int, Rect>();

    string summary = "";

    AnimationClip clip = null;

    public override void OnInspectorGUI() {
        bool targetChanged = false;
        if (target != state) {
            addStatePortionName = "";
            targetChanged = true;
        }
        state = (State) target;

        if (targetChanged) {
            var layer = state.profile.controller.layers[state.profile.controllerLayer];
            var animState = Utils.FindState(layer.stateMachine, state.stateName);
            clip = animState.motion as AnimationClip;

            var sb = new System.Text.StringBuilder();

            var events = AnimationUtility.GetAnimationEvents(clip);
            sb.AppendFormat("Events ({0}) ", events.Length).AppendLine();
            if (events.Length > 0) {
                foreach (var ev in events) {
                    sb.Append(string.Format("{0,4}", (int) FrameUtil.Time2Frame(ev.time))).Append("F ");
                    sb.Append(ev.functionName);
                    sb.AppendLine();
                }
            }
            sb.AppendLine();

            var bindings = AnimationUtility.GetCurveBindings(clip);
            sb.AppendFormat("Bindings ({0})", bindings.Length).AppendLine();
            foreach (var binding in bindings) {
                sb.Append("  ").Append(binding.path).Append(binding.path == "" ? "" : "/")
                    .Append("<").Append(binding.type.Name).Append(">.")
                    .Append(binding.propertyName).AppendLine();
            }

            summary = sb.ToString();
        }

        EditorGUI.BeginChangeCheck();

        EGL.LabelField("Name", state.stateName);
        EGL.LabelField("Frames", state.frames.ToString());

        bool lastGUIEnabled = GUI.enabled;
        GUI.enabled = false;
        EGL.ObjectField("Clip", clip, typeof(AnimationClip), allowSceneObjects: false);
        GUI.enabled = lastGUIEnabled;

        if (summary.Length > 0) {
            EGL.HelpBox(summary, MessageType.None);
        }

        state.tags = InspectTags(state.tags);
        EGL.Space();

        using (new EGL.VerticalScope(EditorStyles.helpBox)) {
            ++EditorGUI.indentLevel;
            InspectBehaviourList(state.behaviours);
            --EditorGUI.indentLevel;
            EGL.Space();
        }

        EGL.LabelField("Portions");
        var portions = state.portions;
        for (int i = 0; i < portions.Count; ++i) {
            var portion = portions[i];

            bool active = activePortions.Contains(portion);
            active = EGL.Foldout(active, portion.name);
            if (active) 
                activePortions.Add(portion);
            else
                activePortions.Remove(portion);

            if (active) {
                ++EditorGUI.indentLevel;
                
                EGL.BeginHorizontal();
                portion.name = EGL.TextField("Name", portion.name);
                // GL.FlexibleSpace();
                if (GL.Button("-", GUILayout.Width(30))) {
                    portions.RemoveAt(i);
                    --i;
                }
                EGL.EndHorizontal();

                portion.range = EditorGUIUtil.FrameRangeSlider("Range", portion.range, state.frames);
                portion.includeEnd = EGL.Toggle("Include nt>=1", portion.includeEnd);
                portion.tags = InspectTags(portion.tags);

                using (new EGL.VerticalScope(EditorStyles.helpBox)) {
                    InspectBehaviourList(portion.behaviours);
                }

                --EditorGUI.indentLevel;
            }
        }

        EGL.Space();
        EGL.Space();
        EGL.BeginHorizontal();
        addStatePortionName = EGL.TextField(addStatePortionName);
        if (GUI.enabled && GL.Button("Add Portion", GL.Width(90))) {
            var portion = new StatePortion {
                name = addStatePortionName
            };
            portions.Add(portion);

            addStatePortionName = "";
        }
        EGL.EndHorizontal();

        if (EditorGUI.EndChangeCheck()) {
            EditorUtility.SetDirty(state);
        }
    }

    string InspectTags(string tags) {
        if (tags == null)
            tags = "";

        tags = EGL.TextField("Tags", tags);
        string[] splitted = tags.Split(',');

        var invalidTags = "";
        foreach (var tag in splitted) {
            if (tag.Length > 0 && !state.profile.tags.Contains(tag)) {
                invalidTags += tag;
                invalidTags += '|';
            }
        }

        if (invalidTags.Length != 0) {
            EGL.HelpBox("Tag |" + invalidTags + " are invalid.", MessageType.Error);
        }
        
        return tags;
    }

    Editor GetBehaviorEditor(StateBehaviour behaviour) {
        if (behaviourEditors.ContainsKey(behaviour)) {
            return behaviourEditors[behaviour];
        } else {
            var editor = Editor.CreateEditor(behaviour);
            behaviourEditors.Add(behaviour, editor);
            return editor;
        }
    }

    void InspectBehaviourList(List<StateBehaviour> list) {
        using (new EGL.HorizontalScope()) {
            EGL.LabelField("Behaviours", EditorStyles.boldLabel);
            GL.FlexibleSpace();
            var listHash = list.GetHashCode();
            if (GL.Button("+", GL.Width(30)) &&
                popupRects.ContainsKey(listHash)) {
                PopupWindow.Show(popupRects[listHash], new AddBehaviourPopup(state.profile, list));
            }

            if (Event.current.type == EventType.Repaint) {
                popupRects.Remove(listHash);
                popupRects.Add(listHash, GUILayoutUtility.GetLastRect());
            }
        }
        for (int i = 0; i < list.Count; ++i) {
            var behaviour = list[i];
            var active = activeBehaviours.Contains(behaviour);

            if (!behaviour) {
                list.RemoveAt(i);
                --i;
                continue;
            }

            EGL.BeginHorizontal();
            active = EGL.Foldout(active, behaviour.name);
            if (GL.Button("-", GL.Width(30))) {
                DestroyImmediate(behaviour, true);
                list.RemoveAt(i);
                --i;
            }
            EGL.EndHorizontal();

            if (!behaviour)
                continue;
            
            if (active) {
                activeBehaviours.Add(behaviour);
            } else {
                activeBehaviours.Remove(behaviour);
            }

            if (active) {
                ++EditorGUI.indentLevel;
                GetBehaviorEditor(behaviour).OnInspectorGUI();
                --EditorGUI.indentLevel;
            }
        }

        if (GUI.enabled) {
            GL.BeginHorizontal();
            GL.FlexibleSpace();


            GL.FlexibleSpace();
            GL.EndHorizontal();
        }
    }

    class AddBehaviourPopup : PopupWindowContent {
        readonly TransitionProfile profile;
        readonly List<StateBehaviour> list;
        readonly SearchField searchField;
        readonly List<string> behaviourTypes = new List<string>();

        string searchText = "";

        Vector2 scrollPosition;

        public AddBehaviourPopup(TransitionProfile _profile, List<StateBehaviour> _list) {
            profile = _profile;
            list = _list;
            searchField = new SearchField();
            searchField.SetFocus();

            var scripts = (MonoScript[]) Resources.FindObjectsOfTypeAll(typeof(MonoScript));
            foreach (var script in scripts) {
                var type = script.GetClass();
                
                if (type != null && typeof(StateBehaviour).IsAssignableFrom(type) && !type.IsAbstract) {
                    behaviourTypes.Add(script.name);
                }
            }
        }

        public override void OnGUI(Rect rect) {
            searchText = searchField.OnGUI(searchText);

            scrollPosition = EGL.BeginScrollView(scrollPosition);

            foreach (var type in behaviourTypes) {
                if (type.ToLower().Contains(searchText.ToLower())) {
                    EGL.BeginHorizontal();
                    EGL.LabelField(type, GL.Width(140));
                    if (GL.Button("Add", GL.Width(40))) {
                        var instance = (StateBehaviour) ScriptableObject.CreateInstance(type);
                        instance.name = type;
                        instance.hideFlags = HideFlags.HideInHierarchy;
                        list.Add(instance);

                        AssetDatabase.AddObjectToAsset(instance, profile);
                        AssetDatabase.SaveAssets();

                        editorWindow.Close();
                    }
                    EGL.EndHorizontal();
                }
            }

            EGL.EndScrollView();
        }

    }

}

}