// Атрибуты PSS. Находятся в Runtime (не в Editor/) чтобы их могли использовать
// и runtime UdonSharp скрипты, и Editor-скрипты.
// Сами атрибуты не содержат runtime-логики — только метаданные для инспектора.

using System;

namespace PuruSignals
{
    // ──────────────────────────────────────────────────────────────────────────
    // [PSS_Module] — регистрирует класс в иерархическом меню PSS.
    // Путь: "Trigger/OnTimer", "Action/SetActive", etc.
    // ──────────────────────────────────────────────────────────────────────────
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PSS_ModuleAttribute : Attribute
    {
        public readonly string menuPath;
        public PSS_ModuleAttribute(string menuPath) { this.menuPath = menuPath; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // [PSS_Field] — описывает поле для PSS_GenericEditor.
    // Редактор рисует его вместо стандартного дефолтного инспектора.
    // ──────────────────────────────────────────────────────────────────────────
    [AttributeUsage(AttributeTargets.Field)]
    public class PSS_FieldAttribute : Attribute
    {
        public readonly string label;
        public readonly float min;
        public readonly float max;
        public readonly bool isList;
        public readonly string showIf;
        public readonly bool canUseDataSlot;
        public readonly string tooltip;

        public PSS_FieldAttribute(string label,
            float min = float.MinValue, float max = float.MaxValue,
            bool isList = false, string showIf = null,
            bool canUseDataSlot = false, string tooltip = null)
        {
            this.label         = label;
            this.min           = min;
            this.max           = max;
            this.isList        = isList;
            this.showIf        = showIf;
            this.canUseDataSlot = canUseDataSlot;
            this.tooltip       = tooltip;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // [PSS_Header] — разделитель/заголовок секции в инспекторе.
    // ──────────────────────────────────────────────────────────────────────────
    [AttributeUsage(AttributeTargets.Field)]
    public class PSS_HeaderAttribute : Attribute
    {
        public readonly string label;
        public PSS_HeaderAttribute(string label) { this.label = label; }
    }
}
