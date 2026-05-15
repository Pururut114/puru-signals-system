using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnExitTrigger")]
    [AddComponentMenu("PSS/Triggers/PSS_OnExitTrigger [Trigger]")]
    public class PSS_OnExitTrigger : PSS_TriggerBase
    {
        [PSS_Field("Local Player Only")]
        public bool localPlayerOnly = true;

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (localPlayerOnly && !player.isLocal) return;
            FireWithPlayer(player);
        }
    }
}
