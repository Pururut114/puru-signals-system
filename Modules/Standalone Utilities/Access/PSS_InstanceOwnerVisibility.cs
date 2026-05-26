using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Access/PSS_InstanceOwnerVisibility [Utility]")]
    [PSS_Note("Enables ownerObjects / disables nonOwnerObjects for the instance owner. Only works in Invite, Friends, and Friends+ instances — false in Public and Group.")]
    public class PSS_InstanceOwnerVisibility : UdonSharpBehaviour
    {
        [Header("NOTE: IsInstanceOwner = false in Public and Group instances")]

        [Header("Enabled for instance owner, disabled for others")]
        public GameObject[] ownerObjects;

        [Header("Disabled for instance owner, enabled for others")]
        public GameObject[] nonOwnerObjects;

        [Header("PSS Integration")]
        [Tooltip("Fires if local player is the instance owner")]
        public PSS_ChannelLocal onOwnerChannel;
        [Tooltip("Fires if local player is not the instance owner")]
        public PSS_ChannelLocal onNonOwnerChannel;

        private void Start()
        {
            VRCPlayerApi lp = Networking.LocalPlayer;
            if (lp == null) return;

            Apply(Networking.IsInstanceOwner);
        }

        private void Apply(bool isOwner)
        {
            for (int i = 0; i < ownerObjects.Length; i++)
                if (ownerObjects[i] != null) ownerObjects[i].SetActive(isOwner);

            for (int i = 0; i < nonOwnerObjects.Length; i++)
                if (nonOwnerObjects[i] != null) nonOwnerObjects[i].SetActive(!isOwner);

            if (isOwner && onOwnerChannel != null) onOwnerChannel.Trigger();
            if (!isOwner && onNonOwnerChannel != null) onNonOwnerChannel.Trigger();
        }
    }
}
