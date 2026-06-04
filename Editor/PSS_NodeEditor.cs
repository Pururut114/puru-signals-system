#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;

namespace PuruSignals.Editor
{
    [CustomEditor(typeof(PSS_Node))]
    public class PSS_NodeEditor : UnityEditor.Editor
    {
        private PSS_Node _node;
        private GUIStyle _headerStyle;

        private static readonly Color ColorNode = new Color(0.65f, 0.45f, 0.95f);

        private void OnEnable()
        {
            _node = (PSS_Node)target;
            if (_node == null) return;

            EditorApplication.delayCall += () =>
            {
                if (_node == null) return;
                EnsureChannel();
                SyncFromComponents();
            };
        }

        public override void OnInspectorGUI()
        {
            if (_node == null) return;

            serializedObject.Update();
            UdonSharpGUI.DrawCompileErrorTextArea();

            // Всегда синхронизируем _channel и syncMode с реальными компонентами
            SyncFromComponents();

            DrawHeader("NODE  /  PSS_Node", ColorNode);
            EditorGUILayout.Space(6);

            DrawSyncMode();

            if (_node.syncMode == NodeSyncMode.Global)
            {
                EditorGUILayout.Space(4);
                DrawNetworkSection();
            }

            EditorGUILayout.Space(8);
            DrawTriggersSection();
            EditorGUILayout.Space(4);
            DrawActionsSection();

            serializedObject.ApplyModifiedProperties();
        }

        // ── State sync ────────────────────────────────────────────────────────

        // Синхронизирует _channel и syncMode с реальным компонентом на объекте.
        // Вызывается каждый OnInspectorGUI чтобы исправить расхождения после
        // DestroyObjectImmediate / domain reload / undo.
        private void SyncFromComponents()
        {
            var ch = _node.gameObject.GetComponent<PSS_ChannelLocal>();
            if (ch == null) return;

            NodeSyncMode realMode = (ch is PSS_ChannelGlobal) ? NodeSyncMode.Global : NodeSyncMode.Local;

            if (_node._channel == ch && _node.syncMode == realMode) return;

            _node._channel = ch;
            _node.syncMode = realMode;
            EditorUtility.SetDirty(_node);
            UdonSharpEditorUtility.CopyProxyToUdon(_node);
        }

        // ── SyncMode ──────────────────────────────────────────────────────────

        private void DrawSyncMode()
        {
            // Читаем из реального компонента, не из _node.syncMode — надёжнее
            var ch = _node.gameObject.GetComponent<PSS_ChannelLocal>();
            int current = (ch is PSS_ChannelGlobal) ? 1 : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sync Mode", GUILayout.Width(84));
            EditorGUI.BeginChangeCheck();
            int newVal = GUILayout.Toolbar(current, new[] { "Local", "Global" });
            if (EditorGUI.EndChangeCheck() && newVal != current)
            {
                serializedObject.ApplyModifiedProperties();
                SwitchChannel((NodeSyncMode)newVal);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SwitchChannel(NodeSyncMode newMode)
        {
            var go = _node.gameObject;

            var oldChannel = go.GetComponent<PSS_ChannelLocal>();
            float savedDelay      = 0f;
            bool  savedRandomize  = false;
            PSS_ActionBase[] savedActions = null;

            if (oldChannel != null)
            {
                savedDelay     = oldChannel.delay;
                savedRandomize = oldChannel.randomize;
                savedActions   = oldChannel._actions;
                Undo.DestroyObjectImmediate(oldChannel);
            }

            PSS_ChannelLocal newChannel;
            if (newMode == NodeSyncMode.Global)
            {
                var global = Undo.AddComponent<PSS_ChannelGlobal>(go);
                var network = FindObjectOfType<PSS_Network>();
                if (network != null)
                {
                    Undo.RecordObject(global, "PSS Auto-Link Network");
                    global.network = network;
                    EditorUtility.SetDirty(global);
                }
                newChannel = global;
            }
            else
            {
                newChannel = Undo.AddComponent<PSS_ChannelLocal>(go);
            }

            Undo.RecordObject(newChannel, "PSS Node Channel Init");
            newChannel.delay     = savedDelay;
            newChannel.randomize = savedRandomize;
            if (savedActions != null) newChannel._actions = savedActions;
            EditorUtility.SetDirty(newChannel);
            UdonSharpEditorUtility.CopyProxyToUdon(newChannel);

            Undo.RecordObject(_node, "PSS Node SyncMode");
            _node.syncMode = newMode;
            _node._channel = newChannel;
            EditorUtility.SetDirty(_node);
            UdonSharpEditorUtility.CopyProxyToUdon(_node);

            // Перепривязка через delayCall — DestroyObjectImmediate + AddComponent
            // требуют следующего frame чтобы UdonSharp proxy корректно инициализировался
            var capturedChannel = newChannel;
            var capturedActions = savedActions;
            EditorApplication.delayCall += () =>
            {
                if (_node == null || capturedChannel == null) return;

                foreach (var t in go.GetComponents<PSS_TriggerBase>())
                {
                    if (t == null) continue;
                    Undo.RecordObject(t, "PSS Node Rewire");
                    t.channel = capturedChannel;
                    EditorUtility.SetDirty(t);
                    UdonSharpEditorUtility.CopyProxyToUdon(t);
                }

                if (capturedActions != null)
                {
                    foreach (var a in capturedActions)
                    {
                        if (a == null) continue;
                        Undo.RecordObject(a, "PSS Node Rewire");
                        a.channel = capturedChannel;
                        EditorUtility.SetDirty(a);
                        UdonSharpEditorUtility.CopyProxyToUdon(a);
                    }
                }

                RescanActions(capturedChannel);
            };
        }

        // ── Network Section ───────────────────────────────────────────────────

        private void DrawNetworkSection()
        {
            var globalChannel = _node.gameObject.GetComponent<PSS_ChannelGlobal>();
            if (globalChannel == null) return;

            if (FindObjectOfType<PSS_Network>() == null)
            {
                EditorGUILayout.HelpBox(
                    "PSS_Network not found in scene — required for Global sync.",
                    MessageType.Warning);

                if (GUILayout.Button("Add PSS_Network to Scene"))
                {
                    var netGo = new GameObject("PSS_Network");
                    Undo.RegisterCreatedObjectUndo(netGo, "Add PSS_Network");
                    var net = Undo.AddComponent<PSS_Network>(netGo);

                    Undo.RecordObject(globalChannel, "PSS Auto-Link Network");
                    globalChannel.network = net;
                    EditorUtility.SetDirty(globalChannel);
                    UdonSharpEditorUtility.CopyProxyToUdon(globalChannel);
                }
            }

            // bufferForLateJoin и network показываем всегда (не только когда сеть найдена)
            var so = new SerializedObject(globalChannel);
            so.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(so.FindProperty("network"),           new GUIContent("Network"));
            EditorGUILayout.PropertyField(so.FindProperty("bufferForLateJoin"), new GUIContent("Buffer for Late Join"));
            if (so.ApplyModifiedProperties() || EditorGUI.EndChangeCheck())
                UdonSharpEditorUtility.CopyProxyToUdon(globalChannel);
        }

        // ── Triggers Section ──────────────────────────────────────────────────

        private void DrawTriggersSection()
        {
            var triggers = _node.gameObject.GetComponents<PSS_TriggerBase>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Triggers", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Trigger", GUILayout.Width(80)))
                ShowAddMenu(typeof(PSS_TriggerBase), AddTrigger);
            EditorGUILayout.EndHorizontal();

            if (triggers.Length == 0)
                EditorGUILayout.LabelField("  — none —", EditorStyles.miniLabel);

            foreach (var t in triggers)
            {
                if (t == null) continue;
                var captured = t;
                DrawModuleRow(t.GetType().Name.Replace("PSS_", ""), () =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (captured != null) Undo.DestroyObjectImmediate(captured);
                    };
                });
            }

            EditorGUILayout.EndVertical();
        }

        // ── Actions Section ───────────────────────────────────────────────────

        private void DrawActionsSection()
        {
            // Читаем из живого компонента, не из proxy — надёжнее после delayCall операций
            var channel = _node.gameObject.GetComponent<PSS_ChannelLocal>();
            var actions = channel?._actions;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("↺", GUILayout.Width(24)))
            {
                if (channel != null) RescanActions(channel);
            }
            if (GUILayout.Button("+ Action", GUILayout.Width(80)))
                ShowAddMenu(typeof(PSS_ActionBase), AddAction);
            EditorGUILayout.EndHorizontal();

            if (actions == null || actions.Length == 0)
                EditorGUILayout.LabelField("  — none —", EditorStyles.miniLabel);
            else
            {
                foreach (var a in actions)
                {
                    if (a == null) continue;
                    var capturedAction  = a;
                    var capturedChannel = channel;
                    DrawModuleRow(a.GetType().Name.Replace("PSS_", ""), () =>
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (capturedAction != null) Undo.DestroyObjectImmediate(capturedAction);
                            if (capturedChannel != null) RescanActions(capturedChannel);
                        };
                    });
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ── Add Menus ─────────────────────────────────────────────────────────

        private void ShowAddMenu(Type baseType, Action<Type> callback)
        {
            var menu = new GenericMenu();
            bool any = false;

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => !t.IsAbstract && t.IsSubclassOf(baseType))
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                var captured = type;
                menu.AddItem(new GUIContent(type.Name.Replace("PSS_", "")), false, () => callback(captured));
                any = true;
            }

            if (!any) menu.AddDisabledItem(new GUIContent("No modules found"));
            menu.ShowAsContext();
        }

        private void AddTrigger(Type type)
        {
            EnsureChannel();
            var channel = _node._channel;
            if (channel == null) return;

            var trigger = (PSS_TriggerBase)Undo.AddComponent(_node.gameObject, type);
            if (trigger == null) return;

            // delayCall: UdonSharp OnEnable вызывает CopyUdonToProxy (обнуляет поля),
            // поэтому привязываем channel только в следующем frame.
            EditorApplication.delayCall += () =>
            {
                if (trigger == null || channel == null) return;
                Undo.RecordObject(trigger, "PSS Node Add Trigger");
                trigger.channel = channel;
                EditorUtility.SetDirty(trigger);
                UdonSharpEditorUtility.CopyProxyToUdon(trigger);
                Repaint();
            };
        }

        private void AddAction(Type type)
        {
            EnsureChannel();
            var channel = _node._channel;
            if (channel == null) return;

            var action = (PSS_ActionBase)Undo.AddComponent(_node.gameObject, type);
            if (action == null) return;

            // delayCall по той же причине — ждём CopyUdonToProxy после OnEnable
            EditorApplication.delayCall += () =>
            {
                if (action == null || channel == null) return;
                Undo.RecordObject(action, "PSS Node Add Action");
                action.channel  = channel;
                action.priority = channel._actions?.Length ?? 0;
                EditorUtility.SetDirty(action);
                UdonSharpEditorUtility.CopyProxyToUdon(action);
                RescanActions(channel);
                Repaint();
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureChannel()
        {
            if (_node._channel != null) return;

            var existing = _node.gameObject.GetComponent<PSS_ChannelLocal>();
            if (existing == null)
            {
                existing = _node.syncMode == NodeSyncMode.Global
                    ? (PSS_ChannelLocal)Undo.AddComponent<PSS_ChannelGlobal>(_node.gameObject)
                    : Undo.AddComponent<PSS_ChannelLocal>(_node.gameObject);
            }

            Undo.RecordObject(_node, "PSS Node Channel");
            _node._channel = existing;
            EditorUtility.SetDirty(_node);
            UdonSharpEditorUtility.CopyProxyToUdon(_node);
        }

        private void RescanActions(PSS_ChannelLocal channel)
        {
            if (channel == null) return;

            var all = FindObjectsOfType<PSS_ActionBase>();
            var linked = all
                .Where(a => a != null && a.channel == channel)
                .OrderBy(a => a.priority)
                .ToArray();

            Undo.RecordObject(channel, "PSS Node Rescan Actions");
            channel._actions = linked;
            EditorUtility.SetDirty(channel);
            UdonSharpEditorUtility.CopyProxyToUdon(channel);
        }

        private void DrawModuleRow(string label, Action onRemove)
        {
            float h = EditorGUIUtility.singleLineHeight;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  " + label, EditorStyles.miniLabel);
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(h)))
                onRemove?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader(string label, Color color)
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.box)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize  = 11,
                    alignment = TextAnchor.MiddleCenter
                };
                _headerStyle.normal.textColor = Color.black;
            }
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Box(label, _headerStyle, GUILayout.ExpandWidth(true), GUILayout.Height(22));
            GUI.backgroundColor = prev;
        }
    }
}
#endif
