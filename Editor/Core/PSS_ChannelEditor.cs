#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;

namespace PuruSignals.Editor
{
    [CustomEditor(typeof(PSS_ChannelLocal), true)]
    public class PSS_ChannelEditor : UnityEditor.Editor
    {
        private PSS_ChannelLocal _channel;
        private ReorderableList _actionsList;
        private List<PSS_ActionBase> _foundActions = new List<PSS_ActionBase>();

        private GUIStyle _headerStyle;
        private static readonly Color ColorChannel = new Color(0.35f, 0.85f, 0.45f);

        private void OnEnable()
        {
            _channel = (PSS_ChannelLocal)target;
            RebuildActionsList();
        }

        public override void OnInspectorGUI()
        {
            if (_channel == null) return;

            serializedObject.Update();
            UdonSharpGUI.DrawCompileErrorTextArea();

            string typeName = target.GetType().Name.Replace("PSS_", "");
            DrawHeader("CHANNEL  /  " + typeName, ColorChannel);

            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"),
                new GUIContent("Delay (sec)", "Задержка перед dispatch в секундах"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomize"),
                new GUIContent("Randomize", "Выбрать один Action по весу вместо всех"));

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Rescan", GUILayout.Width(60)))
                RebuildActionsList();
            EditorGUILayout.EndHorizontal();

            if (_foundActions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Drag a PSS Action component here to link it to this Channel.",
                    MessageType.Info);
            }
            else
            {
                _actionsList?.DoLayoutList();
            }

            // Drag-and-drop зона
            DrawDragDropZone();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDragDropZone()
        {
            EditorGUILayout.Space(4);
            Rect dropRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "+ Drag Action here to link", EditorStyles.helpBox);

            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = GetDraggedAction() != null
                    ? DragAndDropVisualMode.Link
                    : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                PSS_ActionBase action = GetDraggedAction();
                if (action != null)
                {
                    DragAndDrop.AcceptDrag();
                    LinkAction(action);
                    evt.Use();
                }
            }
        }

        private PSS_ActionBase GetDraggedAction()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go)
                {
                    var action = go.GetComponent<PSS_ActionBase>();
                    if (action != null) return action;
                }
                if (obj is PSS_ActionBase a) return a;
            }
            return null;
        }

        private void LinkAction(PSS_ActionBase action)
        {
            if (_foundActions.Contains(action)) return;

            Undo.RecordObject(action, "PSS Link Action");
            action.channel  = _channel;
            action.priority = _foundActions.Count;
            EditorUtility.SetDirty(action);
            UdonSharpEditorUtility.CopyProxyToUdon(action);

            RebuildActionsList();
        }

        private void UnlinkAction(PSS_ActionBase action)
        {
            Undo.RecordObject(action, "PSS Unlink Action");
            action.channel = null;
            EditorUtility.SetDirty(action);
            UdonSharpEditorUtility.CopyProxyToUdon(action);

            RebuildActionsList();
        }

        private void RebuildActionsList()
        {
            if (_channel == null) return;

            var all = FindObjectsOfType<PSS_ActionBase>();
            _foundActions = all
                .Where(a => a != null && a.channel == _channel)
                .OrderBy(a => a.priority)
                .ToList();

            Undo.RecordObject(_channel, "PSS Rescan Actions");
            _channel._actions = _foundActions.ToArray();
            EditorUtility.SetDirty(_channel);
            UdonSharpEditorUtility.CopyProxyToUdon(_channel);

            _actionsList = new ReorderableList(_foundActions, typeof(PSS_ActionBase), true, false, false, true);
            _actionsList.drawHeaderCallback = _ => { };
            _actionsList.drawElementCallback = DrawActionElement;
            _actionsList.onReorderCallbackWithDetails = OnReorder;
            _actionsList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < _foundActions.Count)
                    UnlinkAction(_foundActions[list.index]);
            };
        }

        private void DrawActionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _foundActions.Count) return;
            var action = _foundActions[index];
            if (action == null) return;

            float lineH = EditorGUIUtility.singleLineHeight;
            rect.y += 1;

            Rect prioRect = new Rect(rect.x, rect.y, 28, lineH);
            GUI.Label(prioRect, index.ToString(), EditorStyles.centeredGreyMiniLabel);

            Rect nameRect = new Rect(rect.x + 30, rect.y, rect.width - 120, lineH);
            string name = action.GetType().Name.Replace("PSS_", "");
            GUI.Label(nameRect, name);

            Rect weightLabel = new Rect(rect.x + rect.width - 90, rect.y, 46, lineH);
            Rect weightField = new Rect(rect.x + rect.width - 44, rect.y, 44, lineH);
            GUI.Label(weightLabel, "weight", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            float newWeight = EditorGUI.FloatField(weightField, action.weight);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(action, "PSS Weight");
                action.weight = Mathf.Max(0f, newWeight);
                EditorUtility.SetDirty(action);
                UdonSharpEditorUtility.CopyProxyToUdon(action);
            }
        }

        private void OnReorder(ReorderableList list, int oldIndex, int newIndex)
        {
            for (int i = 0; i < _foundActions.Count; i++)
            {
                if (_foundActions[i] == null) continue;
                Undo.RecordObject(_foundActions[i], "PSS Reorder");
                _foundActions[i].priority = i;
                EditorUtility.SetDirty(_foundActions[i]);
                UdonSharpEditorUtility.CopyProxyToUdon(_foundActions[i]);
            }

            Undo.RecordObject(_channel, "PSS Reorder");
            _channel._actions = _foundActions.ToArray();
            EditorUtility.SetDirty(_channel);
            UdonSharpEditorUtility.CopyProxyToUdon(_channel);
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
