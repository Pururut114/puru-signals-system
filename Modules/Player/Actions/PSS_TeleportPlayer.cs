using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/TeleportPlayer")]
    [AddComponentMenu("PSS/Actions/PSS_TeleportPlayer [Action]")]
    public class PSS_TeleportPlayer : PSS_ActionBase
    {
        [PSS_Field("Target")]
        public Transform target;

        [PSS_Field("Y Offset")]
        public float yOffset = 0f;

        protected override void OnExecute()
        {
            VRCPlayerApi lp = Networking.LocalPlayer;
            if (lp == null || target == null) return;

            Vector3 pos = target.position;
            if (yOffset != 0f) pos.y += yOffset;
            lp.TeleportTo(pos, target.rotation);
        }
    }
}
