using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Channel/PSS_ChannelLocal [Channel]")]
    public class PSS_ChannelLocal : PSS_ModuleBase
    {
        [Header("Dispatch")]
        public float delay = 0f;
        public bool randomize = false;

        // Заполняется PSS_ChannelEditor автоматически при сохранении сцены.
        // Список отсортирован по priority (меньше = первым).
        [HideInInspector] public PSS_ActionBase[] _actions = new PSS_ActionBase[0];

        // Контекст последнего события. Доступен в Actions через channel.triggeredPlayer.
        [HideInInspector] public VRCPlayerApi triggeredPlayer;

        // ── Public API ────────────────────────────────────────────────────────

        public virtual void Trigger()
        {
            triggeredPlayer = null;
            _Dispatch();
        }

        public virtual void TriggerWithPlayer(VRCPlayerApi player)
        {
            triggeredPlayer = player;
            _Dispatch();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void _Dispatch()
        {
            if (delay > 0f)
                SendCustomEventDelayedSeconds(nameof(_Fire), delay);
            else
                _Fire();
        }

        public void _Fire()
        {
            if (_actions == null || _actions.Length == 0) return;

            if (randomize)
                _FireRandom();
            else
                _FireAll();
        }

        private void _FireAll()
        {
            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i] != null)
                    _actions[i].Execute();
            }
        }

        private void _FireRandom()
        {
            float total = 0f;
            for (int i = 0; i < _actions.Length; i++)
                if (_actions[i] != null)
                    total += Mathf.Max(0f, _actions[i].weight);

            if (total <= 0f) return;

            float roll = Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i] == null) continue;
                acc += Mathf.Max(0f, _actions[i].weight);
                if (roll < acc)
                {
                    _actions[i].Execute();
                    return;
                }
            }
            // Fallback на последний (float precision edge)
            for (int i = _actions.Length - 1; i >= 0; i--)
            {
                if (_actions[i] != null) { _actions[i].Execute(); return; }
            }
        }
    }
}
