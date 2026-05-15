using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    public enum StateSyncType { Bool, Int, Float }

    // Хранит синхронизированное состояние. На поздних игроках OnDeserialization
    // применяет РЕАЛЬНОЕ текущее значение — не воспроизводит историю событий.
    // Для Bool: стреляет в channelOnTrue / channelOnFalse.
    // Для Int/Float: пишет в targetSlot (DataSlot → ConditionalTrigger → Channel).

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("PSS/Data/PSS_StateSync [Data]")]
    public class PSS_StateSync : PSS_ModuleBase
    {
        [Header("Type")]
        public StateSyncType valueType = StateSyncType.Bool;

        [Header("Bool — каналы применения состояния")]
        public PSS_ChannelLocal channelOnTrue;
        public PSS_ChannelLocal channelOnFalse;

        [Header("Int / Float — целевой DataSlot")]
        public PSS_DataSlot targetSlot;

        [Header("Options")]
        [Tooltip("Применить текущее состояние при старте (для owner/первого игрока)")]
        public bool applyOnStart = false;

        [UdonSynced] private bool  _syncBool;
        [UdonSynced] private int   _syncInt;
        [UdonSynced] private float _syncFloat;

        private void Start()
        {
            if (applyOnStart) _ApplyState();
        }

        public override void OnDeserialization()
        {
            _ApplyState();
        }

        // ── Write API (вызывается из PSS_SetStateSync) ────────────────────────

        public void SetBool(bool v)
        {
            _syncBool = v;
            _ApplyState();
            _Sync();
        }

        public void Toggle()
        {
            SetBool(!_syncBool);
        }

        public void SetInt(int v)
        {
            _syncInt = v;
            _ApplyState();
            _Sync();
        }

        public void SetFloat(float v)
        {
            _syncFloat = v;
            _ApplyState();
            _Sync();
        }

        // ── Read API ──────────────────────────────────────────────────────────

        public bool  GetBool()  => _syncBool;
        public int   GetInt()   => _syncInt;
        public float GetFloat() => _syncFloat;

        // ── Internal ──────────────────────────────────────────────────────────

        private void _ApplyState()
        {
            switch (valueType)
            {
                case StateSyncType.Bool:
                    if (_syncBool) { if (channelOnTrue  != null) channelOnTrue.Trigger();  }
                    else           { if (channelOnFalse != null) channelOnFalse.Trigger(); }
                    break;

                case StateSyncType.Int:
                    if (targetSlot != null) targetSlot.SetInt(_syncInt);
                    break;

                case StateSyncType.Float:
                    if (targetSlot != null) targetSlot.SetFloat(_syncFloat);
                    break;
            }
        }

        private void _Sync()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
