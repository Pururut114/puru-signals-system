using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Access/PSS_AdminVisibility [Utility]")]
    public class PSS_AdminVisibility : UdonSharpBehaviour
    {
        [Header("Admin display names")]
        public string[] adminNames;

        [Header("Enabled for admins — non-admins: scene defaults (untouched)")]
        public GameObject[] adminObjects;

        [Header("PSS Integration")]
        [Tooltip("Fires if local player is admin")]
        public PSS_ChannelLocal onAdminChannel;

        private void Start()
        {
            VRCPlayerApi lp = Networking.LocalPlayer;
            if (lp == null) return;

            if (!IsAdmin(lp.displayName)) return;

            for (int i = 0; i < adminObjects.Length; i++)
                if (adminObjects[i] != null) adminObjects[i].SetActive(true);

            if (onAdminChannel != null) onAdminChannel.Trigger();
        }

        private bool IsAdmin(string name)
        {
            for (int i = 0; i < adminNames.Length; i++)
                if (adminNames[i] == name) return true;
            return false;
        }
    }
}
