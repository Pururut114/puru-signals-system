#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
using PuruSignals;

namespace PuruSignals.Editor
{
    // Универсальный инспектор для всех наследников PSS_ModuleBase.
    // Рисует:
    //   1. Цветной заголовок (Trigger / Action)
    //   2. Поле Channel (для Trigger) или priority/weight (для Action)
    //   3. Все поля с атрибутом [PSS_Field] в порядке объявления
    //   4. Стандартный compile-error textbox UdonSharp

    [CustomEditor(typeof(PSS_ModuleBase), true)]
    public class PSS_GenericEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, ReorderableList> _lists = new Dictionary<string, ReorderableList>();

        // ── Цвета заголовков ──────────────────────────────────────────────────
        private static readonly Color ColorTrigger = new Color(0.95f, 0.85f, 0.25f);   // жёлтый
        private static readonly Color ColorAction  = new Color(0.25f, 0.75f, 0.95f);   // голубой
        private static readonly Color ColorChannel = new Color(0.35f, 0.85f, 0.45f);   // зелёный

        // ── Кэш стилей ───────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _slotButtonStyle;

        // ── DataSlot toggle state (fieldName → bool) ──────────────────────────
        private readonly Dictionary<string, bool> _useSlot = new Dictionary<string, bool>();

        // ─────────────────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            if (target == null) return;

            serializedObject.Update();
            UdonSharpGUI.DrawCompileErrorTextArea();

            var moduleBase = (PSS_ModuleBase)target;
            bool isTrigger = moduleBase is PSS_TriggerBase;
            bool isAction  = moduleBase is PSS_ActionBase;
            bool isChannel = moduleBase is PSS_ChannelLocal;

            // ── Заголовок ────────────────────────────────────────────────────
            Color headerColor = isChannel ? ColorChannel : (isTrigger ? ColorTrigger : ColorAction);
            string headerLabel = isChannel ? "CHANNEL" : (isTrigger ? "TRIGGER" : "ACTION");
            string typeName = target.GetType().Name.Replace("PSS_", "");
            DrawHeader(headerLabel + "  /  " + typeName, headerColor);

            // ── Channel field (для Trigger) ───────────────────────────────────
            if (isTrigger)
            {
                EditorGUI.BeginChangeCheck();
                var channelProp = serializedObject.FindProperty("channel");
                if (channelProp != null)
                    EditorGUILayout.PropertyField(channelProp, new GUIContent("Channel"));
            }

            // ── Priority / Weight (для Action) ────────────────────────────────
            if (isAction)
            {
                EditorGUI.BeginDisabledGroup(true);
                var priorityProp = serializedObject.FindProperty("priority");
                var weightProp   = serializedObject.FindProperty("weight");
                if (priorityProp != null)
                    EditorGUILayout.PropertyField(priorityProp, new GUIContent("Priority (auto)"));
                EditorGUI.EndDisabledGroup();

                if (weightProp != null)
                    EditorGUILayout.PropertyField(weightProp, new GUIContent("Weight (random)"));

                EditorGUILayout.Space(4);
            }

            // ── PSS_Field атрибуты ────────────────────────────────────────────
            DrawPSSFields(target.GetType());

            serializedObject.ApplyModifiedProperties();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Рисует все поля с [PSS_Field] в иерархии классов (снизу вверх)
        // ─────────────────────────────────────────────────────────────────────
        private void DrawPSSFields(Type type)
        {
            // Собираем поля в правильном порядке: от корня иерархии к конкретному классу
            var fieldList = new List<(FieldInfo field, PSS_FieldAttribute attr, PSS_HeaderAttribute hdr)>();
            CollectFields(type, fieldList);

            foreach (var (field, attr, hdr) in fieldList)
            {
                // Заголовок секции
                if (hdr != null)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(hdr.label, EditorStyles.boldLabel);
                }

                if (attr == null) continue;

                // showIf
                if (!string.IsNullOrEmpty(attr.showIf))
                {
                    var condField = target.GetType().GetField(attr.showIf,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (condField != null && !(bool)condField.GetValue(target))
                        continue;
                }

                var prop = serializedObject.FindProperty(field.Name);
                if (prop == null) continue;

                // DataSlot toggle
                if (attr.canUseDataSlot)
                {
                    DrawFieldWithSlotToggle(field, attr, prop);
                    continue;
                }

                // ReorderableList
                if (attr.isList && prop.isArray)
                {
                    DrawReorderableList(field.Name, prop, attr.label);
                    continue;
                }

                // Float с ограничениями
                if (field.FieldType == typeof(float) &&
                    (attr.min != float.MinValue || attr.max != float.MaxValue))
                {
                    EditorGUI.BeginChangeCheck();
                    float val = EditorGUILayout.FloatField(
                        new GUIContent(attr.label, attr.tooltip), prop.floatValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.floatValue = Mathf.Clamp(val,
                            attr.min == float.MinValue ? float.MinValue : attr.min,
                            attr.max == float.MaxValue ? float.MaxValue : attr.max);
                    continue;
                }

                // Всё остальное — стандартный PropertyField
                EditorGUILayout.PropertyField(prop, new GUIContent(attr.label, attr.tooltip));
            }
        }

        private void CollectFields(Type type, List<(FieldInfo, PSS_FieldAttribute, PSS_HeaderAttribute)> result)
        {
            if (type == null || type == typeof(PSS_ModuleBase) || type == typeof(UnityEngine.MonoBehaviour))
                return;

            CollectFields(type.BaseType, result);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var attr = field.GetCustomAttribute<PSS_FieldAttribute>();
                var hdr  = field.GetCustomAttribute<PSS_HeaderAttribute>();
                if (attr != null || hdr != null)
                    result.Add((field, attr, hdr));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Поле с кнопкой переключения на DataSlot
        // ─────────────────────────────────────────────────────────────────────
        private void DrawFieldWithSlotToggle(FieldInfo field, PSS_FieldAttribute attr, SerializedProperty prop)
        {
            string key = field.Name;
            if (!_useSlot.ContainsKey(key)) _useSlot[key] = false;

            bool useSlot = _useSlot[key];
            string slotFieldName = key + "Slot";
            var slotProp = serializedObject.FindProperty(slotFieldName);

            EditorGUILayout.BeginHorizontal();

            // Кнопка-переключатель
            if (_slotButtonStyle == null)
            {
                _slotButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    normal = { textColor = useSlot ? Color.green : Color.white },
                    fontStyle = FontStyle.Bold
                };
            }
            _slotButtonStyle.normal.textColor = useSlot ? Color.green : new Color(0.7f, 0.7f, 0.7f);

            if (GUILayout.Button("D", _slotButtonStyle, GUILayout.Width(20)))
                _useSlot[key] = !_useSlot[key];

            if (useSlot && slotProp != null)
                EditorGUILayout.PropertyField(slotProp, new GUIContent(attr.label + " [Slot]", attr.tooltip));
            else
                EditorGUILayout.PropertyField(prop, new GUIContent(attr.label, attr.tooltip));

            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ReorderableList с кэшем
        // ─────────────────────────────────────────────────────────────────────
        private void DrawReorderableList(string key, SerializedProperty prop, string label)
        {
            if (!_lists.TryGetValue(key, out var list))
            {
                list = new ReorderableList(serializedObject, prop, true, true, true, true);
                list.drawHeaderCallback = r => EditorGUI.LabelField(r, label);
                list.drawElementCallback = (rect, idx, active, focused) =>
                {
                    rect.y += 1; rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, prop.GetArrayElementAtIndex(idx), GUIContent.none);
                };
                _lists[key] = list;
            }
            list.DoLayoutList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Цветной заголовок
        // ─────────────────────────────────────────────────────────────────────
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

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Box(label, _headerStyle, GUILayout.ExpandWidth(true), GUILayout.Height(22));
            GUI.backgroundColor = prevColor;
        }
    }
}
#endif
