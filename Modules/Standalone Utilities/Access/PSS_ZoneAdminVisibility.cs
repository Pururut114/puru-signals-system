using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Access/PSS_ZoneAdminVisibility [Utility]")]
    public class PSS_ZoneAdminVisibility : UdonSharpBehaviour
    {
        [Header("Admin display names")]
        public string[] adminNames;

        [Header("Enabled for admins on zone entry — non-admins: scene defaults (untouched)")]
        public GameObject[] adminObjects;

        [Header("Zone trigger colliders (empty = auto-collect from self/children)")]
        public Collider[] zoneColliders;

        [Header("PSS Integration")]
        [Tooltip("Fires once when admin enters the zone")]
        public PSS_ChannelLocal onAdminChannel;

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

            if (!IsAdmin(player.displayName)) return;

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
