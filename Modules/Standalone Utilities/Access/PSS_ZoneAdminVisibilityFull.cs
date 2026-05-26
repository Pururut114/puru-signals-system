using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Access/PSS_ZoneAdminVisibilityFull [Utility]")]
    [PSS_Note("Enables adminObjects and disables nonAdminObjects for admins on first zone entry; inverted for non-admins.")]
    public class PSS_ZoneAdminVisibilityFull : UdonSharpBehaviour
    {
        [Header("Admin display names")]
        public string[] adminNames;

        [Header("Admin state: enabled for admins, disabled for non-admins")]
        public GameObject[] adminObjects;

        [Header("Non-admin state: disabled for admins, enabled for non-admins")]
        public GameObject[] nonAdminObjects;

        [Header("Zone trigger colliders (empty = auto-collect from self/children)")]
        public Collider[] zoneColliders;

        [Header("PSS Integration")]
        [Tooltip("Fires once when admin enters the zone")]
        public PSS_ChannelLocal onAdminChannel;
        [Tooltip("Fires once when non-admin enters the zone")]
        public PSS_ChannelLocal onNonAdminChannel;

        private bool _checked;

        private void Start()
        {
            if (zoneColliders == null || zoneColliders.Length == 0)
                AutoCollectColliders();
        }

        private void AutoCollectColliders()
        {
            Collider[] all = GetComponentsInChildren<Collider>();
            int count = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i].isTrigger) count++;
            if (count == 0) return;
            zoneColliders = new Collider[count];
            int idx = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i].isTrigger) zoneColliders[idx++] = all[i];
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (_checked || !player.isLocal) return;
            _checked = true;

            Apply(IsAdmin(player.displayName));
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
