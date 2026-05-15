using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    // Срабатывает когда значение DataSlot удовлетворяет условию.
    // Режимы оценки:
    //   OnChange  — EvaluateCondition() вызывается автоматически при изменении DataSlot (подписка через _listeners)
    //   OnUpdate  — EvaluateCondition() вызывается каждый кадр (Update)
    //   Manual    — EvaluateCondition() вызывать вручную из Udon или PSS_ActiveConditionalTrigger

    public enum ConditionOp
    {
        Equal, NotEqual,
        GreaterThan, GreaterOrEqual,
        LessThan, LessOrEqual,
        IsTrue, IsFalse
    }

    public enum EvalMode { OnChange, OnUpdate, Manual }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/ConditionalTrigger")]
    [AddComponentMenu("PSS/Triggers/PSS_ConditionalTrigger [Trigger]")]
    public class PSS_ConditionalTrigger : PSS_TriggerBase
    {
        [PSS_Field("Source Slot", tooltip: "DataSlot значение которого проверяется")]
        public PSS_DataSlot sourceSlot;

        [PSS_Field("Condition")]
        public ConditionOp condition = ConditionOp.GreaterThan;

        [PSS_Header("Threshold")]
        [PSS_Field("Int Threshold")]   public int    thresholdInt;
        [PSS_Field("Float Threshold")] public float  thresholdFloat;
        [PSS_Field("String Threshold")]public string thresholdString = "";

        [PSS_Header("Behaviour")]
        [PSS_Field("Evaluation Mode")]
        public EvalMode evalMode = EvalMode.OnChange;

        [PSS_Field("Fire Once", tooltip: "Не срабатывать повторно, пока условие не сбросится (false)")]
        public bool fireOnce = true;

        private bool _lastState = false;

        private void Start()
        {
            if (evalMode == EvalMode.OnChange && sourceSlot != null)
            {
                var listeners = sourceSlot._listeners;
                var newArr = new PSS_ConditionalTrigger[listeners.Length + 1];
                for (int i = 0; i < listeners.Length; i++) newArr[i] = listeners[i];
                newArr[listeners.Length] = this;
                sourceSlot._listeners = newArr;
            }
        }

        private void Update()
        {
            if (evalMode == EvalMode.OnUpdate)
                EvaluateCondition();
        }

        public void EvaluateCondition()
        {
            if (sourceSlot == null) return;

            bool result = Evaluate();

            if (result)
            {
                if (fireOnce && _lastState) return;
                _lastState = true;
                Fire();
            }
            else
            {
                _lastState = false;
            }
        }

        private bool Evaluate()
        {
            if (sourceSlot == null) return false;

            switch (sourceSlot.valueType)
            {
                case DataSlotType.Bool:
                    switch (condition)
                    {
                        case ConditionOp.IsTrue:  return sourceSlot.GetBool();
                        case ConditionOp.IsFalse: return !sourceSlot.GetBool();
                        case ConditionOp.Equal:   return sourceSlot.GetBool() == (thresholdInt != 0);
                        default: return false;
                    }

                case DataSlotType.Int:
                    int iv = sourceSlot.GetInt();
                    switch (condition)
                    {
                        case ConditionOp.Equal:          return iv == thresholdInt;
                        case ConditionOp.NotEqual:       return iv != thresholdInt;
                        case ConditionOp.GreaterThan:    return iv >  thresholdInt;
                        case ConditionOp.GreaterOrEqual: return iv >= thresholdInt;
                        case ConditionOp.LessThan:       return iv <  thresholdInt;
                        case ConditionOp.LessOrEqual:    return iv <= thresholdInt;
                        default: return false;
                    }

                case DataSlotType.Float:
                    float fv = sourceSlot.GetFloat();
                    switch (condition)
                    {
                        case ConditionOp.Equal:          return Mathf.Approximately(fv, thresholdFloat);
                        case ConditionOp.NotEqual:       return !Mathf.Approximately(fv, thresholdFloat);
                        case ConditionOp.GreaterThan:    return fv >  thresholdFloat;
                        case ConditionOp.GreaterOrEqual: return fv >= thresholdFloat;
                        case ConditionOp.LessThan:       return fv <  thresholdFloat;
                        case ConditionOp.LessOrEqual:    return fv <= thresholdFloat;
                        default: return false;
                    }

                case DataSlotType.String:
                    string sv = sourceSlot.GetString();
                    switch (condition)
                    {
                        case ConditionOp.Equal:    return sv == thresholdString;
                        case ConditionOp.NotEqual: return sv != thresholdString;
                        default: return false;
                    }

                default:
                    return false;
            }
        }
    }
}
