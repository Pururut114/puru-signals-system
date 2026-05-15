using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    // Именной триггер. Вызывается через:
    //   1. Action PSS_ActiveCustomTrigger (по ссылке или имени)
    //   2. Прямой вызов Activate() из любого Udon-скрипта

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/CustomTrigger")]
    [AddComponentMenu("PSS/Triggers/PSS_CustomTrigger [Trigger]")]
    public class PSS_CustomTrigger : PSS_TriggerBase
    {
        [PSS_Field("Name", tooltip: "Уникальное имя для вызова через PSS_ActiveCustomTrigger")]
        public string triggerName = "";

        // Вызвать напрямую из другого Udon-скрипта
        public void Activate()
        {
            Fire();
        }

        public void ActivateWithPlayer(VRCPlayerApi player)
        {
            FireWithPlayer(player);
        }
    }
}
