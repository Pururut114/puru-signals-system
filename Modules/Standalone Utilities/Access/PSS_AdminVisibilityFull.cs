using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Access/PSS_AdminVisibilityFull [Utility]")]
    public class PSS_AdminVisibilityFull : UdonSharpBehaviour
    {
        [Header("Admin display names")]
        public string[] adminNames;

        [Header("Admin state: enabled for admins, disabled for non-admins")]
        public GameObject[] adminObjects;

        [Header("Non-admin state: disabled for admins, enabled for non-admins")]
        public GameObject[] nonAdminObjects;

        [Header("PSS Integration")]
        [Tooltip("Fires if local player is admin")]
        public PSS_ChannelLocal onAdminChannel;
        [Tooltip("Fires if local player is not admin")]
        public PSS_ChannelLocal onNonAdminChannel;

        private void Start()
        {
            VRCPlayerApi lp = Networking.LocalPlayer;
            if (lp == null) return;

            Apply(IsAdmin(lp.displayName));
        }

        private bool IsAdmin(string name)
        {
            for (int i = 0; i < adminNames.Length; i++)
                if (adminNames[i] == name) return true;
            return false;
        }

        private void Apply(bool isAdmin)
        {
            for (int i = 0; i < adminObjects.Length; i++)
                if (adminObjects[i] != null) adminObjects[i].SetActive(isAdmin);

            for (int i = 0; i < nonAdminObjects.Length; i++)
                if (nonAdminObjects[i] != null) nonAdminObjects[i].SetActive(!isAdmin);

            if (isAdmin && onAdminChannel != null) onAdminChannel.Trigger();
            if (!isAdmin && onNonAdminChannel != null) onNonAdminChannel.Trigger();
        }
    }
}
