using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace PuruSignals
{
    public enum CallNetworkMode { Local, All, Owner }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/CallMethod")]
    [AddComponentMenu("PSS/Actions/PSS_CallMethod [Action]")]
    public class PSS_CallMethod : PSS_ActionBase
    {
        [PSS_Field("Target Behaviour")]
        public UdonBehaviour target;

        [PSS_Field("Event Name", tooltip: "Имя публичного метода (без скобок)")]
        public string eventName = "";

        [PSS_Field("Network Mode")]
        public CallNetworkMode networkMode = CallNetworkMode.Local;

        protected override void OnExecute()
        {
            if (target == null || string.IsNullOrEmpty(eventName)) return;

            switch (networkMode)
            {
                case CallNetworkMode.Local:
                    target.SendCustomEvent(eventName);
                    break;
                case CallNetworkMode.All:
                    target.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, eventName);
                    break;
                case CallNetworkMode.Owner:
                    target.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, eventName);
                    break;
            }
        }
    }
}
