using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnSpawn")]
    [AddComponentMenu("PSS/Triggers/PSS_OnSpawn [Trigger]")]
    public class PSS_OnSpawn : PSS_TriggerBase
    {
        [PSS_Field("Delay (sec)", min: 0f, tooltip: "Задержка перед срабатыванием после Start")]
        public float delay = 0f;

        private void Start()
        {
            if (delay > 0f)
                SendCustomEventDelayedSeconds(nameof(_FireDelayed), delay);
            else
                Fire();
        }

        public void _FireDelayed() => Fire();
    }
}
