using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnEnterTrigger")]
    [AddComponentMenu("PSS/Triggers/PSS_OnEnterTrigger [Trigger]")]
    public class PSS_OnEnterTrigger : PSS_TriggerBase
    {
        [PSS_Field("Local Player Only")]
        public bool localPlayerOnly = true;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (localPlayerOnly && !player.isLocal) return;
            FireWithPlayer(player);
        }
    }
}
