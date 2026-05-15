using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnEnable")]
    [AddComponentMenu("PSS/Triggers/PSS_OnEnable [Trigger]")]
    public class PSS_OnEnable : PSS_TriggerBase
    {
        [PSS_Field("Skip First Enable", tooltip: "Не срабатывать при первом включении (при старте сцены)")]
        public bool skipFirst = false;

        private bool _firstDone = false;

        private void OnEnable()
        {
            if (skipFirst && !_firstDone) { _firstDone = true; return; }
            _firstDone = true;
            Fire();
        }
    }
}
