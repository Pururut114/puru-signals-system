using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/ActiveCustomTrigger")]
    [AddComponentMenu("PSS/Actions/PSS_ActiveCustomTrigger [Action]")]
    public class PSS_ActiveCustomTrigger : PSS_ActionBase
    {
        [PSS_Header("Target")]
        [PSS_Field("Trigger", tooltip: "Прямая ссылка на PSS_CustomTrigger")]
        public PSS_CustomTrigger targetTrigger;

        [PSS_Header("Name Search (fallback)")]
        [PSS_Field("Candidates", isList: true, tooltip: "Список PSS_CustomTrigger для поиска по имени. Назначить вручную.")]
        public PSS_CustomTrigger[] candidates;

        [PSS_Field("Trigger Name", tooltip: "Ищет первый PSS_CustomTrigger из candidates с совпадающим triggerName")]
        public string triggerName = "";

        [PSS_Field("Pass Player Context", tooltip: "Передать triggeredPlayer из текущего события")]
        public bool passPlayer = false;

        protected override void OnExecute()
        {
            VRCPlayerApi player = passPlayer ? channel.triggeredPlayer : null;

            // Прямая ссылка
            if (targetTrigger != null)
            {
                if (player != null) targetTrigger.ActivateWithPlayer(player);
                else                targetTrigger.Activate();
                return;
            }

            // Поиск по имени в candidates
            if (string.IsNullOrEmpty(triggerName) || candidates == null) return;
            for (int i = 0; i < candidates.Length; i++)
            {
                var t = candidates[i];
                if (t == null || t.triggerName != triggerName) continue;
                if (player != null) t.ActivateWithPlayer(player);
                else                t.Activate();
                return;
            }
        }
    }
}
