using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Persistence;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Persistence/PSS_PositionPersistence [Utility]")]
    public class PSS_PositionPersistence : UdonSharpBehaviour
    {
        [Header("Save Settings")]
        [Tooltip("Prefix for PlayerData keys. Unique per world — rarely needs changing.")]
        public string keyPrefix = "PSS";

        [Tooltip("Seconds between automatic position saves. Ignored when useCheckpointsOnly is true.")]
        public float saveInterval = 10f;

        [Tooltip("Disable auto-save loop. Only save when OnCheckpointReached() is called externally.")]
        public bool useCheckpointsOnly = false;

        [Header("PSS Integration (optional)")]
        [Tooltip("Channel fired after the player is teleported to their last saved position.")]
        public PSS_ChannelLocal onRestoredChannel;

        private string _posKey;
        private string _rotKey;
        private VRCPlayerApi _lp;
        private bool _restored;

        private void Start()
        {
            _lp     = Networking.LocalPlayer;
            _posKey = keyPrefix + "-LastPos";
            _rotKey = keyPrefix + "-LastRot";

            if (!useCheckpointsOnly && saveInterval > 0f)
                SendCustomEventDelayedSeconds(nameof(_SaveLoop), saveInterval);
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal) return;
            _restored = true;

            Vector3    pos;
            Quaternion rot;
            bool hasPos = PlayerData.TryGetVector3(player,    _posKey, out pos);
            bool hasRot = PlayerData.TryGetQuaternion(player, _rotKey, out rot);

            if (!hasPos) return;

            player.TeleportTo(pos, hasRot ? rot : player.GetRotation());

            if (onRestoredChannel != null) onRestoredChannel.Trigger();
        }

        public void _SaveLoop()
        {
            if (_restored && Utilities.IsValid(_lp))
                SaveCurrentPosition();

            if (!useCheckpointsOnly && saveInterval > 0f)
                SendCustomEventDelayedSeconds(nameof(_SaveLoop), saveInterval);
        }

        // Call from checkpoint triggers to save at specific points
        public void OnCheckpointReached()
        {
            SaveCurrentPosition();
        }

        public void SaveCurrentPosition()
        {
            if (!_restored || !Utilities.IsValid(_lp)) return;
            PlayerData.SetVector3(_posKey,    _lp.GetPosition());
            PlayerData.SetQuaternion(_rotKey, _lp.GetRotation());
        }
    }
}
