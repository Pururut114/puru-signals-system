using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnDisable")]
    [AddComponentMenu("PSS/Triggers/PSS_OnDisable [Trigger]")]
    public class PSS_OnDisable : PSS_TriggerBase
    {
        private void OnDisable()
        {
            Fire();
        }
    }
}
