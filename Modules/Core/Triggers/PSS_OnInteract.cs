using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Trigger/OnInteract")]
    [AddComponentMenu("PSS/Triggers/PSS_OnInteract [Trigger]")]
    // Объект становится интерактивным автоматически при наличии Interact(). VRC_Interactable не нужен.
    public class PSS_OnInteract : PSS_TriggerBase
    {
        public override void Interact()
        {
            FireWithPlayer(Networking.LocalPlayer);
        }
    }
}
