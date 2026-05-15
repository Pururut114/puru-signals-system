using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    // Лёгкий контейнер данных. Хранит одно значение одного типа.
    // Используется Actions и ConditionalTrigger для чтения/записи динамических значений.

    public enum DataSlotType { Bool, Int, Float, Vector3, String }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Data/PSS_DataSlot [Data]")]
    public class PSS_DataSlot : PSS_ModuleBase
    {
        [Header("Type")]
        public DataSlotType valueType = DataSlotType.Float;

        [Header("Value")]
        public bool    valueBool;
        public int     valueInt;
        public float   valueFloat;
        public Vector3 valueVec3;
        public string  valueString = "";

        // ── Getters ───────────────────────────────────────────────────────────

        public bool    GetBool()   => valueBool;
        public int     GetInt()    => valueInt;
        public float   GetFloat()  => valueFloat;
        public Vector3 GetVec3()   => valueVec3;
        public string  GetString() => valueString;

        // ── Setters (вызывают OnChanged) ──────────────────────────────────────

        public void SetBool(bool v)
        {
            valueBool = v;
            _OnChanged();
        }

        public void SetInt(int v)
        {
            valueInt = v;
            _OnChanged();
        }

        public void SetFloat(float v)
        {
            valueFloat = v;
            _OnChanged();
        }

        public void SetVec3(Vector3 v)
        {
            valueVec3 = v;
            _OnChanged();
        }

        public void SetString(string v)
        {
            valueString = v;
            _OnChanged();
        }

        // ── Listeners ─────────────────────────────────────────────────────────
        // ConditionalTrigger подписывается сюда чтобы реагировать на изменения.
        // Заполняется редактором (PSS_DataSlotEditor).

        [HideInInspector] public PSS_ConditionalTrigger[] _listeners = new PSS_ConditionalTrigger[0];

        public void _OnChanged()
        {
            for (int i = 0; i < _listeners.Length; i++)
                if (_listeners[i] != null)
                    _listeners[i].EvaluateCondition();
        }

        // ── Arithmetic helpers (для PSS_SetDataSlot) ─────────────────────────

        public void AddInt(int v)   => SetInt(valueInt + v);
        public void AddFloat(float v) => SetFloat(valueFloat + v);
        public void SubInt(int v)   => SetInt(valueInt - v);
        public void SubFloat(float v) => SetFloat(valueFloat - v);
        public void MulInt(int v)   => SetInt(valueInt * v);
        public void MulFloat(float v) => SetFloat(valueFloat * v);
        public void DivInt(int v)   { if (v != 0) SetInt(valueInt / v); }
        public void DivFloat(float v) { if (v != 0f) SetFloat(valueFloat / v); }
    }
}
