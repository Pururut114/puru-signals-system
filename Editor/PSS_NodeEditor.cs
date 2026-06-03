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

            var existing = _node.gameObject.GetComponent<PSS_ChannelBase>();

            if (existing == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (_node == null) return;
                    EnsureChannel();
                };
            }
            else if (_node._channel == null)
            {
                Undo.RecordObject(_node, "PSS Node Link Channel");
                _node._channel = existing;
                EditorUtility.SetDirty(_node);
                UdonSharpEditorUtility.CopyProxyToUdon(_node);
            }
        }

        public override void OnInspectorGUI()
        {
            if (_node == null) return;

            serializedObject.Update();
            UdonSharpGUI.DrawCompileErrorTextArea();

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

        // ── SyncMode ──────────────────────────────────────────────────────────

        private void DrawSyncMode()
        {
            int current = (int)_node.syncMode;

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

            var oldChannel = go.GetComponent<PSS_ChannelBase>();
            float savedDelay = 0f;
            bool savedRandomize = false;
            PSS_ActionBase[] savedActions = null;

            if (oldChannel != null)
            {
                savedDelay    = oldChannel.delay;
                savedRandomize = oldChannel.randomize;
                savedActions  = oldChannel._actions;
                Undo.DestroyObjectImmediate(oldChannel);
            }

            PSS_ChannelBase newChannel;
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

            var triggers = go.GetComponents<PSS_TriggerBase>();
            foreach (var t in triggers)
            {
                if (t == null) continue;
                Undo.RecordObject(t, "PSS Node Rewire");
                t.channel = newChannel;
                EditorUtility.SetDirty(t);
                UdonSharpEditorUtility.CopyProxyToUdon(t);
            }

            if (savedActions != null)
            {
                foreach (var a in savedActions)
                {
                    if (a == null) continue;
                    Undo.RecordObject(a, "PSS Node Rewire");
                    a.channel = newChannel;
                    EditorUtility.SetDirty(a);
                    UdonSharpEditorUtility.CopyProxyToUdon(a);
                }
            }
        }

        // ── Network Section ───────────────────────────────────────────────────

        private void DrawNetworkSection()
        {
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

                    var global = _node.gameObject.GetComponent<PSS_ChannelGlobal>();
                    if (global != null)
                    {
                        Undo.RecordObject(global, "PSS Auto-Link Network");
                        global.network = net;
                        EditorUtility.SetDirty(global);
                        UdonSharpEditorUtility.CopyProxyToUdon(global);
                    }
                }
            }

            var globalChannel = _node.gameObject.GetComponent<PSS_ChannelGlobal>();
            if (globalChannel == null) return;

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
            var actions = _node._channel?._actions;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
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
                    var capturedAction = a;
                    var capturedChannel = _node._channel;
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
            var trigger = (PSS_TriggerBase)Undo.AddComponent(_node.gameObject, type);
            if (trigger == null || _node._channel == null) return;

            Undo.RecordObject(trigger, "PSS Node Add Trigger");
            trigger.channel = _node._channel;
            EditorUtility.SetDirty(trigger);
            UdonSharpEditorUtility.CopyProxyToUdon(trigger);
        }

        private void AddAction(Type type)
        {
            EnsureChannel();
            var action = (PSS_ActionBase)Undo.AddComponent(_node.gameObject, type);
            if (action == null || _node._channel == null) return;

            Undo.RecordObject(action, "PSS Node Add Action");
            action.channel  = _node._channel;
            action.priority = _node._channel._actions?.Length ?? 0;
            EditorUtility.SetDirty(action);
            UdonSharpEditorUtility.CopyProxyToUdon(action);

            RescanActions(_node._channel);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureChannel()
        {
            if (_node._channel != null) return;

            var existing = _node.gameObject.GetComponent<PSS_ChannelBase>();
            if (existing == null)
            {
                existing = _node.syncMode == NodeSyncMode.Global
                    ? (PSS_ChannelBase)Undo.AddComponent<PSS_ChannelGlobal>(_node.gameObject)
                    : Undo.AddComponent<PSS_ChannelLocal>(_node.gameObject);
            }

            Undo.RecordObject(_node, "PSS Node Channel");
            _node._channel = existing;
            EditorUtility.SetDirty(_node);
            UdonSharpEditorUtility.CopyProxyToUdon(_node);
        }

        private void RescanActions(PSS_ChannelBase channel)
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
