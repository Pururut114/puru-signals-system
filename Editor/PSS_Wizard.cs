#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;

namespace PuruSignals.Editor
{
    public class PSS_Wizard : EditorWindow
    {
        // ── Sync Mode ─────────────────────────────────────────────────────────
        enum SyncMode { Local, Global, GlobalState }
        SyncMode _syncMode = SyncMode.Local;

        PSS_Network _foundNetwork;
        bool        _networkSearched;

        // ── Trigger ──────────────────────────────────────────────────────────
        enum TriggerKind { OnInteract, OnEnterTrigger, OnExitTrigger, OnTimer, OnSpawn }
        TriggerKind _triggerKind = TriggerKind.OnInteract;

        bool  _localPlayerOnly  = true;
        float _timerInterval    = 1f;
        bool  _timerRepeat      = true;
        bool  _timerRandom      = false;
        float _timerMaxInterval = 2f;
        float _spawnDelay       = 0f;

        // ── Channel ───────────────────────────────────────────────────────────
        float _channelDelay = 0f;

        // ── GlobalState ───────────────────────────────────────────────────────
        StateSyncType _stateSyncType = StateSyncType.Bool;

        // Int/Float — write side
        StateSyncNumOp _writeNumOp      = StateSyncNumOp.Add;
        int            _writeValueInt   = 1;
        float          _writeValueFloat = 1f;

        // Int/Float — condition (reaction)
        ConditionOp _condOp             = ConditionOp.GreaterOrEqual;
        int         _condThresholdInt   = 0;
        float       _condThresholdFloat = 0f;
        bool        _condFireOnce       = true;

        // ── Action ────────────────────────────────────────────────────────────
        enum ActionKind
        {
            SetActive, AnimationParam, CallMethod,
#if PSS_LTCGI_INSTALLED
            LtcgiControl,
#endif
            TeleportPlayer
        }
        ActionKind _actionKind = ActionKind.SetActive;

        GameObject    _setActiveTarget;
        SetActiveOp   _setActiveOp    = SetActiveOp.Toggle;

        Animator      _animator;
        string        _paramName  = "";
        AnimParamType _paramType  = AnimParamType.Trigger;
        bool          _paramBool  = true;
        int           _paramInt   = 0;
        float         _paramFloat = 0f;

        UdonBehaviour   _callTarget;
        string          _callEvent   = "";
        CallNetworkMode _callNetMode = CallNetworkMode.Local;

#if PSS_LTCGI_INSTALLED
        UdonSharpBehaviour _ltcgiAdapter;
        LtcgiMode _ltcgiMode      = LtcgiMode.Global;
        LtcgiOp   _ltcgiOperation = LtcgiOp.Toggle;
#endif

        Transform _teleportTarget;
        float     _teleportYOffset = 0f;

        // ── Root object ───────────────────────────────────────────────────────
        GameObject _rootObject;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/PSS/Quick Setup...")]
        public static void Open()
        {
            var win = GetWindow<PSS_Wizard>(true, "PSS Quick Setup", true);
            win.minSize = new Vector2(320, 580);
            win.maxSize = new Vector2(420, 920);
            if (Selection.activeGameObject != null)
                win._rootObject = Selection.activeGameObject;
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);

            // Objects
            Section("Objects");
            _rootObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Root Object", "Объект куда добавятся компоненты. Пусто = создать новый."),
                _rootObject, typeof(GameObject), true);

            EditorGUILayout.Space(8);

            // Trigger
            Section("Trigger");
            _triggerKind = (TriggerKind)EditorGUILayout.EnumPopup("Type", _triggerKind);
            DrawTriggerFields();

            EditorGUILayout.Space(8);

            // Channel / Sync
            Section("Channel");
            EditorGUI.BeginChangeCheck();
            _syncMode = (SyncMode)EditorGUILayout.EnumPopup("Sync Mode", _syncMode);
            if (EditorGUI.EndChangeCheck())
                _networkSearched = false;

            _channelDelay = Mathf.Max(0f, EditorGUILayout.FloatField("Delay (sec)", _channelDelay));

            if (_syncMode == SyncMode.Global)
                DrawNetworkStatus();
            else if (_syncMode == SyncMode.GlobalState)
                DrawGlobalStateChannelFields();

            // Write + Condition только для Int/Float GlobalState
            if (_syncMode == SyncMode.GlobalState && _stateSyncType != StateSyncType.Bool)
            {
                EditorGUILayout.Space(8);
                DrawWriteSection();
                EditorGUILayout.Space(8);
                DrawConditionSection();
            }

            EditorGUILayout.Space(8);

            // Action
            bool isBoolState    = _syncMode == SyncMode.GlobalState && _stateSyncType == StateSyncType.Bool;
            bool isNumericState = _syncMode == SyncMode.GlobalState && _stateSyncType != StateSyncType.Bool;
            string actionLabel  = isBoolState ? "Action on State Change"
                                : isNumericState ? "Action on Condition"
                                : "Action";
            Section(actionLabel);
            _actionKind = (ActionKind)EditorGUILayout.EnumPopup("Type", _actionKind);
            DrawActionFields();

            GUILayout.FlexibleSpace();

            // Summary
            EditorGUILayout.HelpBox(BuildSummary(), MessageType.None);
            EditorGUILayout.Space(4);

            bool canCreate = CanCreate();
            GUI.enabled = canCreate;
            GUI.backgroundColor = canCreate ? new Color(0.4f, 0.85f, 0.5f) : new Color(0.55f, 0.55f, 0.55f);
            if (GUILayout.Button("Create Chain", GUILayout.Height(30)))
                Create();
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        private string BuildSummary()
        {
            if (_syncMode == SyncMode.Global)
                return $"{_triggerKind}  →  ChannelGlobal  →  {_actionKind}";
            if (_syncMode == SyncMode.GlobalState)
            {
                if (_stateSyncType == StateSyncType.Bool)
                    return $"{_triggerKind}  →  StateSync(Bool)  →  {_actionKind}(T/F)";
                return $"{_triggerKind}  →  StateSync({_stateSyncType})  →  DataSlot  →  {_condOp}  →  {_actionKind}";
            }
            return $"{_triggerKind}  →  Channel  →  {_actionKind}";
        }

        // ── GlobalState channel fields ────────────────────────────────────────

        private void DrawGlobalStateChannelFields()
        {
            _stateSyncType = (StateSyncType)EditorGUILayout.EnumPopup("State Type", _stateSyncType);

            if (_stateSyncType == StateSyncType.Bool)
                EditorGUILayout.HelpBox(
                    "Sync через [UdonSynced]. PSS_Network не нужен.\n" +
                    "Лейт-джоинеры получают актуальное состояние автоматически.",
                    MessageType.Info);
            else
                EditorGUILayout.HelpBox(
                    $"StateSync({_stateSyncType}) пишет в DataSlot.\n" +
                    "ConditionalTrigger реагирует на изменение локально у каждого.",
                    MessageType.Info);
        }

        // ── Write section (Int/Float GlobalState) ─────────────────────────────

        private void DrawWriteSection()
        {
            Section("Write");
            _writeNumOp = (StateSyncNumOp)EditorGUILayout.EnumPopup("Operation", _writeNumOp);
            if (_stateSyncType == StateSyncType.Int)
                _writeValueInt   = EditorGUILayout.IntField("Value", _writeValueInt);
            else
                _writeValueFloat = EditorGUILayout.FloatField("Value", _writeValueFloat);
        }

        // ── Condition section (Int/Float GlobalState) ─────────────────────────

        private void DrawConditionSection()
        {
            Section("Condition");
            _condOp = (ConditionOp)EditorGUILayout.EnumPopup("Condition", _condOp);
            if (_stateSyncType == StateSyncType.Int)
                _condThresholdInt   = EditorGUILayout.IntField("Threshold", _condThresholdInt);
            else
                _condThresholdFloat = EditorGUILayout.FloatField("Threshold", _condThresholdFloat);
            _condFireOnce = EditorGUILayout.Toggle("Fire Once", _condFireOnce);
        }

        // ── Network status UI ─────────────────────────────────────────────────

        private void DrawNetworkStatus()
        {
            if (!_networkSearched)
            {
                _foundNetwork    = FindObjectOfType<PSS_Network>();
                _networkSearched = true;
            }

            EditorGUILayout.Space(3);

            if (_foundNetwork != null)
            {
                var prev = GUI.color;
                GUI.color = new Color(0.45f, 1f, 0.55f);
                EditorGUILayout.HelpBox($"PSS_Network: найден  [{_foundNetwork.gameObject.name}]", MessageType.None);
                GUI.color = prev;
            }
            else
            {
                EditorGUILayout.HelpBox("PSS_Network: не найден в сцене. Нужен для Global.", MessageType.Warning);
                GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
                if (GUILayout.Button("Создать PSS_Network в сцене"))
                {
                    var go = new GameObject("PSS_Network");
                    Undo.RegisterCreatedObjectUndo(go, "Create PSS_Network");
                    go.AddComponent<PSS_Network>();
                    EditorUtility.SetDirty(go);
                    _networkSearched = false;
                    Debug.Log("[PSS Wizard] Создан PSS_Network");
                    Repaint();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(2);
        }

        private bool CanCreate()
        {
            if (_syncMode == SyncMode.Global)
            {
                if (!_networkSearched) { _foundNetwork = FindObjectOfType<PSS_Network>(); _networkSearched = true; }
                return _foundNetwork != null;
            }
            return true;
        }

        // ── Field drawers ─────────────────────────────────────────────────────

        private void DrawTriggerFields()
        {
            switch (_triggerKind)
            {
                case TriggerKind.OnEnterTrigger:
                case TriggerKind.OnExitTrigger:
                    _localPlayerOnly = EditorGUILayout.Toggle("Local Player Only", _localPlayerOnly);
                    break;
                case TriggerKind.OnTimer:
                    _timerInterval    = Mathf.Max(0.01f, EditorGUILayout.FloatField("Interval (sec)", _timerInterval));
                    _timerRepeat      = EditorGUILayout.Toggle("Repeat", _timerRepeat);
                    _timerRandom      = EditorGUILayout.Toggle("Random Range", _timerRandom);
                    if (_timerRandom)
                        _timerMaxInterval = Mathf.Max(_timerInterval, EditorGUILayout.FloatField("Max Interval", _timerMaxInterval));
                    break;
                case TriggerKind.OnSpawn:
                    _spawnDelay = Mathf.Max(0f, EditorGUILayout.FloatField("Delay (sec)", _spawnDelay));
                    break;
            }
        }

        private void DrawActionFields()
        {
            bool isBoolState = _syncMode == SyncMode.GlobalState && _stateSyncType == StateSyncType.Bool;

            switch (_actionKind)
            {
                case ActionKind.SetActive:
                    _setActiveTarget = (GameObject)EditorGUILayout.ObjectField(
                        new GUIContent("Target", "Оставь пустым — подставится Root Object"),
                        _setActiveTarget, typeof(GameObject), true);
                    if (!isBoolState)
                        _setActiveOp = (SetActiveOp)EditorGUILayout.EnumPopup("Operation", _setActiveOp);
                    else
                        EditorGUILayout.HelpBox("Operation: True/False  (авто по состоянию)", MessageType.None);
                    break;

                case ActionKind.AnimationParam:
                    _animator  = (Animator)EditorGUILayout.ObjectField("Animator", _animator, typeof(Animator), true);
                    _paramName = EditorGUILayout.TextField("Param Name", _paramName);
                    _paramType = (AnimParamType)EditorGUILayout.EnumPopup("Param Type", _paramType);
                    if (_paramType == AnimParamType.Bool)
                    {
                        if (isBoolState)
                            EditorGUILayout.HelpBox("Bool Value: True/False  (авто по состоянию)", MessageType.None);
                        else
                            _paramBool = EditorGUILayout.Toggle("Bool Value", _paramBool);
                    }
                    if (_paramType == AnimParamType.Int)   _paramInt   = EditorGUILayout.IntField("Int Value", _paramInt);
                    if (_paramType == AnimParamType.Float) _paramFloat = EditorGUILayout.FloatField("Float Value", _paramFloat);
                    break;

                case ActionKind.CallMethod:
                    _callTarget  = (UdonBehaviour)EditorGUILayout.ObjectField("Target", _callTarget, typeof(UdonBehaviour), true);
                    _callEvent   = EditorGUILayout.TextField("Event Name", _callEvent);
                    _callNetMode = (CallNetworkMode)EditorGUILayout.EnumPopup("Network Mode", _callNetMode);
                    if (isBoolState)
                        EditorGUILayout.HelpBox("Вызывается при любом изменении состояния (True и False).", MessageType.None);
                    break;

#if PSS_LTCGI_INSTALLED
                case ActionKind.LtcgiControl:
                    _ltcgiAdapter = (UdonSharpBehaviour)EditorGUILayout.ObjectField(
                        new GUIContent("LTCGI Adapter", "Перетащи LTCGI_UdonAdapter из сцены"),
                        _ltcgiAdapter, typeof(UdonSharpBehaviour), true);
                    _ltcgiMode = (LtcgiMode)EditorGUILayout.EnumPopup("Mode", _ltcgiMode);
                    if (!isBoolState)
                        _ltcgiOperation = (LtcgiOp)EditorGUILayout.EnumPopup("Operation", _ltcgiOperation);
                    else
                        EditorGUILayout.HelpBox("Operation: True/False  (авто по состоянию)", MessageType.None);
                    break;
#endif

                case ActionKind.TeleportPlayer:
                    _teleportTarget = (Transform)EditorGUILayout.ObjectField(
                        new GUIContent("Target", "Transform точки назначения"),
                        _teleportTarget, typeof(Transform), true);
                    _teleportYOffset = EditorGUILayout.FloatField("Y Offset", _teleportYOffset);
                    if (isBoolState)
                        EditorGUILayout.HelpBox("Телепортирует на одну точку при True и False.", MessageType.None);
                    break;
            }
        }

        // ── Create dispatch ───────────────────────────────────────────────────

        private void Create()
        {
            switch (_syncMode)
            {
                case SyncMode.Local:  CreateLocal();  break;
                case SyncMode.Global: CreateGlobal(); break;
                case SyncMode.GlobalState:
                    if (_stateSyncType == StateSyncType.Bool)
                        CreateGlobalStateBool();
                    else
                        CreateGlobalStateNumeric();
                    break;
            }
        }

        // ── CreateLocal ───────────────────────────────────────────────────────

        private void CreateLocal()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("PSS Quick Setup");
            int group = Undo.GetCurrentGroup();

            GameObject root = EnsureRoot();

            PSS_ChannelLocal channel = root.AddComponent<PSS_ChannelLocal>();
            channel.delay = _channelDelay;
            EditorUtility.SetDirty(channel);

            PSS_TriggerBase trigger = (PSS_TriggerBase)root.AddComponent(TriggerType());
            trigger.channel = channel;
            ApplyTriggerFields(trigger);
            EditorUtility.SetDirty(trigger);

            GameObject actionObj = ActionTargetObject() ?? root;
            PSS_ActionBase action = (PSS_ActionBase)actionObj.AddComponent(ActionType());
            action.channel  = channel;
            action.priority = 0;
            ApplyActionFields(action, root);
            EditorUtility.SetDirty(action);

            channel._actions = new PSS_ActionBase[] { action };
            EditorUtility.SetDirty(channel);

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            Debug.Log($"[PSS Wizard] Local: {TriggerType().Name} → ChannelLocal → {ActionType().Name} на {root.name}");
            Close();
        }

        // ── CreateGlobal ──────────────────────────────────────────────────────

        private void CreateGlobal()
        {
            if (!_networkSearched) { _foundNetwork = FindObjectOfType<PSS_Network>(); _networkSearched = true; }
            if (_foundNetwork == null) { Debug.LogError("[PSS Wizard] PSS_Network не найден!"); return; }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("PSS Quick Setup (Global)");
            int group = Undo.GetCurrentGroup();

            GameObject root = EnsureRoot();

            PSS_ChannelGlobal channel = root.AddComponent<PSS_ChannelGlobal>();
            channel.delay   = _channelDelay;
            channel.network = _foundNetwork;
            EditorUtility.SetDirty(channel);

            PSS_TriggerBase trigger = (PSS_TriggerBase)root.AddComponent(TriggerType());
            trigger.channel = channel;
            ApplyTriggerFields(trigger);
            EditorUtility.SetDirty(trigger);

            GameObject actionObj = ActionTargetObject() ?? root;
            PSS_ActionBase action = (PSS_ActionBase)actionObj.AddComponent(ActionType());
            action.channel  = channel;
            action.priority = 0;
            ApplyActionFields(action, root);
            EditorUtility.SetDirty(action);

            channel._actions = new PSS_ActionBase[] { action };
            EditorUtility.SetDirty(channel);

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            Debug.Log($"[PSS Wizard] Global: {TriggerType().Name} → ChannelGlobal → {ActionType().Name} на {root.name}");
            Close();
        }

        // ── CreateGlobalStateBool ─────────────────────────────────────────────
        //
        //   [Root]  Trigger → buttonChannel → SetStateSync(Toggle)
        //   [_Sync] StateSync(Bool)
        //             channelOnTrue  → Action(True)
        //             channelOnFalse → Action(False)

        private void CreateGlobalStateBool()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("PSS Quick Setup (GlobalState Bool)");
            int group = Undo.GetCurrentGroup();

            GameObject root = EnsureRoot();

            PSS_ChannelLocal buttonChannel = root.AddComponent<PSS_ChannelLocal>();
            buttonChannel.delay = _channelDelay;
            EditorUtility.SetDirty(buttonChannel);

            PSS_TriggerBase trigger = (PSS_TriggerBase)root.AddComponent(TriggerType());
            trigger.channel = buttonChannel;
            ApplyTriggerFields(trigger);
            EditorUtility.SetDirty(trigger);

            GameObject syncObj = new GameObject(root.name + "_Sync");
            Undo.RegisterCreatedObjectUndo(syncObj, "");
            syncObj.transform.SetParent(root.transform, false);

            PSS_ChannelLocal trueChannel  = syncObj.AddComponent<PSS_ChannelLocal>();
            PSS_ChannelLocal falseChannel = syncObj.AddComponent<PSS_ChannelLocal>();

            PSS_ActionBase actionTrue  = (PSS_ActionBase)syncObj.AddComponent(ActionType());
            PSS_ActionBase actionFalse = (PSS_ActionBase)syncObj.AddComponent(ActionType());

            actionTrue.channel   = trueChannel;
            actionTrue.priority  = 0;
            actionFalse.channel  = falseChannel;
            actionFalse.priority = 0;

            ApplyActionFieldsState(actionTrue,  true,  root);
            ApplyActionFieldsState(actionFalse, false, root);

            trueChannel._actions  = new PSS_ActionBase[] { actionTrue  };
            falseChannel._actions = new PSS_ActionBase[] { actionFalse };

            EditorUtility.SetDirty(trueChannel);
            EditorUtility.SetDirty(falseChannel);
            EditorUtility.SetDirty(actionTrue);
            EditorUtility.SetDirty(actionFalse);

            PSS_StateSync stateSync = syncObj.AddComponent<PSS_StateSync>();
            stateSync.valueType      = StateSyncType.Bool;
            stateSync.channelOnTrue  = trueChannel;
            stateSync.channelOnFalse = falseChannel;
            stateSync.applyOnStart   = false;
            EditorUtility.SetDirty(stateSync);

            PSS_SetStateSync setSync = root.AddComponent<PSS_SetStateSync>();
            setSync.channel  = buttonChannel;
            setSync.priority = 0;
            setSync.target   = stateSync;
            SetField(setSync, "boolOp", StateSyncBoolOp.Toggle);
            EditorUtility.SetDirty(setSync);

            buttonChannel._actions = new PSS_ActionBase[] { setSync };
            EditorUtility.SetDirty(buttonChannel);

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            Debug.Log($"[PSS Wizard] GlobalState Bool: {TriggerType().Name} → StateSync → {ActionType().Name}(T/F) на {root.name}");
            Close();
        }

        // ── CreateGlobalStateNumeric ──────────────────────────────────────────
        //
        //   [Root]  Trigger → buttonChannel → SetStateSync(numOp, value)
        //   [_Sync] StateSync(Int/Float, targetSlot=dataSlot)
        //           DataSlot(Int/Float)
        //           ConditionalTrigger(condition, threshold) → reactionChannel → Action

        private void CreateGlobalStateNumeric()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"PSS Quick Setup (GlobalState {_stateSyncType})");
            int group = Undo.GetCurrentGroup();

            GameObject root = EnsureRoot();

            PSS_ChannelLocal buttonChannel = root.AddComponent<PSS_ChannelLocal>();
            buttonChannel.delay = _channelDelay;
            EditorUtility.SetDirty(buttonChannel);

            PSS_TriggerBase trigger = (PSS_TriggerBase)root.AddComponent(TriggerType());
            trigger.channel = buttonChannel;
            ApplyTriggerFields(trigger);
            EditorUtility.SetDirty(trigger);

            GameObject syncObj = new GameObject(root.name + "_Sync");
            Undo.RegisterCreatedObjectUndo(syncObj, "");
            syncObj.transform.SetParent(root.transform, false);

            // DataSlot
            PSS_DataSlot dataSlot = syncObj.AddComponent<PSS_DataSlot>();
            dataSlot.valueType = _stateSyncType == StateSyncType.Int ? DataSlotType.Int : DataSlotType.Float;
            EditorUtility.SetDirty(dataSlot);

            // StateSync → DataSlot
            PSS_StateSync stateSync = syncObj.AddComponent<PSS_StateSync>();
            stateSync.valueType    = _stateSyncType;
            stateSync.targetSlot   = dataSlot;
            stateSync.applyOnStart = false;
            EditorUtility.SetDirty(stateSync);

            // Reaction channel + action
            PSS_ChannelLocal reactionChannel = syncObj.AddComponent<PSS_ChannelLocal>();

            PSS_ActionBase action = (PSS_ActionBase)syncObj.AddComponent(ActionType());
            action.channel  = reactionChannel;
            action.priority = 0;
            ApplyActionFields(action, root);
            EditorUtility.SetDirty(action);

            reactionChannel._actions = new PSS_ActionBase[] { action };
            EditorUtility.SetDirty(reactionChannel);

            // ConditionalTrigger
            PSS_ConditionalTrigger condTrigger = syncObj.AddComponent<PSS_ConditionalTrigger>();
            condTrigger.sourceSlot = dataSlot;
            condTrigger.condition  = _condOp;
            condTrigger.evalMode   = EvalMode.OnChange;
            condTrigger.fireOnce   = _condFireOnce;
            condTrigger.channel    = reactionChannel;
            if (_stateSyncType == StateSyncType.Int)
                condTrigger.thresholdInt   = _condThresholdInt;
            else
                condTrigger.thresholdFloat = _condThresholdFloat;
            EditorUtility.SetDirty(condTrigger);

            // SetStateSync на root
            PSS_SetStateSync setSync = root.AddComponent<PSS_SetStateSync>();
            setSync.channel  = buttonChannel;
            setSync.priority = 0;
            setSync.target   = stateSync;
            SetField(setSync, "numOp", _writeNumOp);
            if (_stateSyncType == StateSyncType.Int)
                SetField(setSync, "valueInt",   _writeValueInt);
            else
                SetField(setSync, "valueFloat", _writeValueFloat);
            EditorUtility.SetDirty(setSync);

            buttonChannel._actions = new PSS_ActionBase[] { setSync };
            EditorUtility.SetDirty(buttonChannel);

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            Debug.Log($"[PSS Wizard] GlobalState {_stateSyncType}: {TriggerType().Name} → StateSync → DataSlot → {_condOp} → {ActionType().Name} на {root.name}");
            Close();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private GameObject EnsureRoot()
        {
            if (_rootObject != null) return _rootObject;
            var go = new GameObject("PSS_" + _triggerKind.ToString());
            Undo.RegisterCreatedObjectUndo(go, "");
            if (Selection.activeTransform != null)
                go.transform.SetParent(Selection.activeTransform.parent, false);
            return go;
        }

        private Type TriggerType()
        {
            switch (_triggerKind)
            {
                case TriggerKind.OnInteract:     return typeof(PSS_OnInteract);
                case TriggerKind.OnEnterTrigger: return typeof(PSS_OnEnterTrigger);
                case TriggerKind.OnExitTrigger:  return typeof(PSS_OnExitTrigger);
                case TriggerKind.OnTimer:        return typeof(PSS_OnTimer);
                case TriggerKind.OnSpawn:        return typeof(PSS_OnSpawn);
                default:                         return typeof(PSS_OnInteract);
            }
        }

        private Type ActionType()
        {
            switch (_actionKind)
            {
                case ActionKind.SetActive:      return typeof(PSS_SetActive);
                case ActionKind.AnimationParam: return typeof(PSS_AnimationParam);
                case ActionKind.CallMethod:     return typeof(PSS_CallMethod);
#if PSS_LTCGI_INSTALLED
                case ActionKind.LtcgiControl:   return typeof(PSS_LtcgiControl);
#endif
                case ActionKind.TeleportPlayer: return typeof(PSS_TeleportPlayer);
                default:                        return typeof(PSS_SetActive);
            }
        }

        private GameObject ActionTargetObject()
        {
            switch (_actionKind)
            {
                case ActionKind.SetActive:      return _setActiveTarget;
                case ActionKind.AnimationParam: return _animator != null ? _animator.gameObject : null;
                default:                        return null;
            }
        }

        // ── Field appliers ────────────────────────────────────────────────────

        private void ApplyTriggerFields(PSS_TriggerBase trigger)
        {
            switch (_triggerKind)
            {
                case TriggerKind.OnEnterTrigger:
                case TriggerKind.OnExitTrigger:
                    SetField(trigger, "localPlayerOnly", _localPlayerOnly);
                    break;
                case TriggerKind.OnTimer:
                    SetField(trigger, "interval",    _timerInterval);
                    SetField(trigger, "repeat",      _timerRepeat);
                    SetField(trigger, "randomRange", _timerRandom);
                    SetField(trigger, "intervalMax", _timerMaxInterval);
                    break;
                case TriggerKind.OnSpawn:
                    SetField(trigger, "delay", _spawnDelay);
                    break;
            }
        }

        private void ApplyActionFields(PSS_ActionBase action, GameObject fallback)
        {
            switch (_actionKind)
            {
                case ActionKind.SetActive:
                    GameObject saTarget = _setActiveTarget != null ? _setActiveTarget : fallback;
                    SetField(action, "targets",   new GameObject[] { saTarget });
                    SetField(action, "operation", _setActiveOp);
                    break;

                case ActionKind.AnimationParam:
                    if (_animator != null)
                        SetField(action, "targets", new Animator[] { _animator });
                    SetField(action, "paramName",  _paramName);
                    SetField(action, "paramType",  _paramType);
                    SetField(action, "valueBool",  _paramBool);
                    SetField(action, "valueInt",   _paramInt);
                    SetField(action, "valueFloat", _paramFloat);
                    break;

                case ActionKind.CallMethod:
                    if (_callTarget != null)
                        SetField(action, "target", _callTarget);
                    SetField(action, "eventName",   _callEvent);
                    SetField(action, "networkMode", _callNetMode);
                    break;

#if PSS_LTCGI_INSTALLED
                case ActionKind.LtcgiControl:
                    if (_ltcgiAdapter != null)
                        SetField(action, "adapter", _ltcgiAdapter);
                    SetField(action, "mode",      _ltcgiMode);
                    SetField(action, "operation", _ltcgiOperation);
                    break;
#endif

                case ActionKind.TeleportPlayer:
                    if (_teleportTarget != null)
                        SetField(action, "target",  _teleportTarget);
                    SetField(action, "yOffset", _teleportYOffset);
                    break;
            }
        }

        // Bool GlobalState: True/False варианты одного экшена
        private void ApplyActionFieldsState(PSS_ActionBase action, bool stateTrue, GameObject fallback)
        {
            switch (_actionKind)
            {
                case ActionKind.SetActive:
                    GameObject saTarget = _setActiveTarget != null ? _setActiveTarget : fallback;
                    SetField(action, "targets",   new GameObject[] { saTarget });
                    SetField(action, "operation", stateTrue ? SetActiveOp.True : SetActiveOp.False);
                    break;

                case ActionKind.AnimationParam:
                    if (_animator != null)
                        SetField(action, "targets", new Animator[] { _animator });
                    SetField(action, "paramName",  _paramName);
                    SetField(action, "paramType",  _paramType);
                    // Bool: авто true/false; остальные типы: одинаковое значение в обоих каналах
                    SetField(action, "valueBool",  _paramType == AnimParamType.Bool ? stateTrue : _paramBool);
                    SetField(action, "valueInt",   _paramInt);
                    SetField(action, "valueFloat", _paramFloat);
                    break;

                case ActionKind.CallMethod:
                    if (_callTarget != null)
                        SetField(action, "target", _callTarget);
                    SetField(action, "eventName",   _callEvent);
                    SetField(action, "networkMode", _callNetMode);
                    break;

#if PSS_LTCGI_INSTALLED
                case ActionKind.LtcgiControl:
                    if (_ltcgiAdapter != null)
                        SetField(action, "adapter", _ltcgiAdapter);
                    SetField(action, "mode",      _ltcgiMode);
                    SetField(action, "operation", stateTrue ? LtcgiOp.True : LtcgiOp.False);
                    break;
#endif

                case ActionKind.TeleportPlayer:
                    if (_teleportTarget != null)
                        SetField(action, "target",  _teleportTarget);
                    SetField(action, "yOffset", _teleportYOffset);
                    break;
            }
        }

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object obj, string name, object value)
        {
            Type t = obj.GetType();
            while (t != null)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) { f.SetValue(obj, value); return; }
                t = t.BaseType;
            }
            Debug.LogWarning($"[PSS Wizard] поле '{name}' не найдено в {obj.GetType().Name}");
        }

        // ── UI helper ─────────────────────────────────────────────────────────

        private static void Section(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            Rect r = GUILayoutUtility.GetLastRect();
            r.y += r.height;
            r.height = 1;
            EditorGUI.DrawRect(r, new Color(1f, 1f, 1f, 0.15f));
            GUILayout.Space(2);
        }
    }
}
#endif
