using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/SetAvatarScale")]
    [AddComponentMenu("PSS/Actions/PSS_SetAvatarScale [Action]")]
    public class PSS_SetAvatarScale : PSS_ActionBase
    {
        [PSS_Field("Set specific height", tooltip: "true = мир задаёт точный рост; false = задать min/max для player-controlled scaling")]
        public bool setSpecificHeight = true;

        [PSS_Field("Eye height (m)", min: 0.1f, max: 100f, showIf: "setSpecificHeight")]
        public float eyeHeight = 1.8f;

        [PSS_Field("Allow manual scaling", tooltip: "Разрешить игроку менять рост вручную (только при setSpecificHeight = false)")]
        public bool allowManualScaling = true;

        [PSS_Field("Min height (m)", min: 0.2f, max: 5f)]
        public float minHeight = 0.2f;

        [PSS_Field("Max height (m)", min: 0.2f, max: 5f)]
        public float maxHeight = 5f;

        protected override void OnExecute()
        {
            var lp = Networking.LocalPlayer;
            if (lp == null) return;

            if (setSpecificHeight)
            {
                // SetManualAvatarScalingAllowed(false) должен идти ДО SetAvatarEyeHeightByMeters,
                // иначе последующее включение manual scaling сбрасывает рост к исходному (баг VRC)
                lp.SetManualAvatarScalingAllowed(false);
                lp.SetAvatarEyeHeightByMeters(Mathf.Clamp(eyeHeight, 0.1f, 100f));
            }
            else
            {
                lp.SetManualAvatarScalingAllowed(allowManualScaling);
                lp.SetAvatarEyeHeightMinimumByMeters(Mathf.Clamp(minHeight, 0.2f, 5f));
                lp.SetAvatarEyeHeightMaximumByMeters(Mathf.Clamp(maxHeight, 0.2f, 5f));
            }
        }
    }
}
