#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using PuruSignals;

namespace PuruSignals.Editor
{
    // Контекстное меню в Hierarchy и меню GameObject/PSS.
    // Позволяет быстро добавить Channel, Trigger, Action на выбранный GameObject.

    public static class PSS_HierarchyMenu
    {
        // ── GameObject меню ───────────────────────────────────────────────────

        [MenuItem("GameObject/PSS/Add Channel (Local)", false, 10)]
        private static void AddChannelLocal()
        {
            AddComponentToSelection<PSS_ChannelLocal>("Channel Local");
        }

        [MenuItem("GameObject/PSS/Add Trigger...", false, 11)]
        private static void AddTrigger()
        {
            ShowModuleMenu(typeof(PSS_TriggerBase));
        }

        [MenuItem("GameObject/PSS/Add Action...", false, 12)]
        private static void AddAction()
        {
            ShowModuleMenu(typeof(PSS_ActionBase));
        }

        // ── Hierarchy правый клик ─────────────────────────────────────────────

        [MenuItem("CONTEXT/PSS_ChannelLocal/Rescan Actions")]
        private static void RescanActionsContext(MenuCommand cmd)
        {
            // Открываем инспектор — редактор сам пересканирует
            Selection.activeObject = ((Component)cmd.context).gameObject;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Показывает GenericMenu со всеми зарегистрированными модулями
        // указанного базового типа.
        // ─────────────────────────────────────────────────────────────────────
        private static void ShowModuleMenu(Type baseType)
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("[PSS] Select a GameObject first.");
                return;
            }

            var menu = new GenericMenu();
            var modules = GetModuleTypes(baseType);

            if (modules.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No modules found"));
            }
            else
            {
                foreach (var (path, type) in modules)
                {
                    var capturedType = type;
                    menu.AddItem(new GUIContent(path), false, () =>
                    {
                        AddComponentToSelection(capturedType, path);
                    });
                }
            }

            menu.ShowAsContext();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Сканирует все сборки на наличие классов с [PSS_Module],
        // наследующих baseType.
        // ─────────────────────────────────────────────────────────────────────
        public static List<(string path, Type type)> GetModuleTypes(Type baseType)
        {
            var result = new List<(string path, Type type)>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract) continue;
                        if (!baseType.IsAssignableFrom(type)) continue;

                        var attr = type.GetCustomAttribute<PSS_ModuleAttribute>();
                        if (attr == null) continue;

                        result.Add((attr.menuPath, type));
                    }
                }
                catch { /* пропустить сборки без доступа */ }
            }

            result.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────

        private static void AddComponentToSelection<T>(string undoLabel) where T : Component
        {
            AddComponentToSelection(typeof(T), undoLabel);
        }

        private static void AddComponentToSelection(Type type, string undoLabel)
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            Undo.AddComponent(go, type);

            // Если это UdonSharpBehaviour — создать program asset если его нет
            if (typeof(UdonSharp.UdonSharpBehaviour).IsAssignableFrom(type))
            {
                var comp = go.GetComponent(type) as UdonSharp.UdonSharpBehaviour;
                if (comp != null)
                    UdonSharpEditorUtility.GetBackingUdonBehaviour(comp);
            }
        }
    }
}
#endif
